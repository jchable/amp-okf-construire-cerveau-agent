// Run from the repository root after building Acme.Console and tests/TeamsTurn in Release.
// The loopback endpoint scripts model decisions. The SDK, OKF tools and filesystem are real.
import assert from 'node:assert/strict';
import { randomUUID } from 'node:crypto';
import { spawn } from 'node:child_process';
import { mkdtemp, readdir, readFile } from 'node:fs/promises';
import http from 'node:http';
import { tmpdir } from 'node:os';
import { join } from 'node:path';

const memory = await mkdtemp(join(tmpdir(), 'amp-rehearsal-'));
const tenant = randomUUID();
const presenter = randomUUID();
const original = 'Format ' + randomUUID();
const replacement = 'Format ' + randomUUID();
let steps = [];
let failure;

function tool(name, args) {
    return { role: 'assistant', content: null, tool_calls: [{
        id: randomUUID(), type: 'function', function: { name, arguments: JSON.stringify(args) }
    }] };
}
function answer(content) { return { role: 'assistant', content }; }

const server = http.createServer(async (request, response) => {
    try {
        assert.equal(request.url, '/v1/chat/completions');
        const chunks = [];
        for await (const chunk of request) chunks.push(chunk);
        const payload = JSON.parse(Buffer.concat(chunks));
        assert.ok(!payload.tools.some(t => t.function.name === 'okf_write_concept'));
        const step = steps.shift();
        assert.ok(step, 'Unexpected model request or repeated Teams turn');
        const message = step(payload.messages.at(-1), payload);
        response.writeHead(200, { 'Content-Type': 'application/json' });
        response.end(JSON.stringify({
            id: randomUUID(), object: 'chat.completion', created: 1, model: 'test',
            choices: [{ index: 0, message, finish_reason: message.tool_calls ? 'tool_calls' : 'stop' }]
        }));
    } catch (error) {
        failure = error;
        response.writeHead(400);
        response.end();
    }
});
await new Promise((resolve, reject) => server.once('error', reject).listen(0, '127.0.0.1', resolve));

async function run(project, prompt, script, user = presenter) {
    steps = [...script];
    const child = spawn('dotnet', ['run', '--project', project, '-c', 'Release', '--no-build', '--', prompt], {
        windowsHide: true, env: { ...process.env,
            OKF_CHAT_BASE_URL: 'http://127.0.0.1:' + server.address().port + '/v1',
            OKF_CHAT_MODEL: 'test', OKF_CHAT_API_KEY: 'loopback',
            ACME_TENANT_ID: tenant, ACME_USER_ID: user, OKF_MEMORY_ROOT: memory
        }
    });
    let output = '';
    child.stdout.on('data', data => output += data);
    child.stderr.on('data', data => output += data);
    const code = await new Promise((resolve, reject) => child.once('error', reject).once('close', resolve));
    if (failure) throw failure;
    assert.equal(code, 0, output);
    assert.equal(steps.length, 0, output);
    return output;
}

const preparationEnv = {
    ...process.env,
    OKF_CHAT_BASE_URL: 'http://127.0.0.1:' + server.address().port + '/v1',
    OKF_CHAT_MODEL: 'test', OKF_CHAT_API_KEY: 'loopback',
    ACME_TENANT_ID: tenant, ACME_USER_ID: presenter,
    OKF_MEMORY_ROOT: memory, ACME_BOT_ID: randomUUID(), ACME_BOT_SECRET: 'local-test'
};

async function prepare(demo) {
    const args = ['-NoProfile', '-File', './scripts/Prepare-Demo' + demo + '.ps1'];
    if (demo === 3) args.push('-PublicUrl', 'https://acme-test.example.com');
    const child = spawn('pwsh', args, {
        windowsHide: true, env: preparationEnv, timeout: 180_000
    });
    let output = '';
    child.stdout.on('data', data => output += data);
    child.stderr.on('data', data => output += data);
    const code = await new Promise((resolve, reject) =>
        child.once('error', reject).once('close', resolve));
    assert.equal(code, 0, output);
}

async function memorySnapshot() {
    const files = (await readdir(memory, { recursive: true, withFileTypes: true }))
        .filter(entry => entry.isFile());
    const contents = await Promise.all(files.map(async entry => [
        join(entry.parentPath, entry.name),
        (await readFile(join(entry.parentPath, entry.name))).toString('base64')
    ]));
    return contents.sort(([a], [b]) => a.localeCompare(b));
}

const cli = 'src/Acme.Console';
try {
    if (process.argv.includes('--prepare')) await prepare(2);
    const policy = await run(cli, 'Une commande Acme livrée hier est-elle comptabilisée ?', [
        () => tool('okf_search', { query: 'revenue' }),
        last => {
            assert.match(last.content, /policies\/revenue-recognition/);
            return tool('okf_read_concept', { conceptId: 'policies/revenue-recognition' });
        },
        last => { assert.match(last.content, /30 days/); return answer('Source : policies/revenue-recognition'); }
    ]);
    assert.match(policy, /okf_read_concept policies\/revenue-recognition/);
    for (const preference of [original, replacement]) {
        await run(cli, 'Mémorise ma préférence : ' + preference, [
            () => tool('memoriser_preference', { preference }),
            last => { assert.doesNotMatch(last.content, /Error:|Exception/); return answer('Enregistrée.'); }
        ]);
    }
    if (process.argv.includes('--prepare')) {
        const before = await memorySnapshot();
        assert.ok(before.length > 0, 'La console doit avoir écrit la préférence.');
        for (let pass = 0; pass < 2; pass++) {
            await prepare(2);
            await prepare(3);
            assert.deepEqual(await memorySnapshot(), before, 'La préparation a modifié la mémoire.');
        }
        console.log('OK : préparations répétées, mémoire conservée.');
    }
    for (const project of [cli, 'tests/TeamsTurn']) {
        const result = await run(project, 'Prépare mon briefing chiffre d’affaires.', [
            () => tool('lire_preference', {}),
            last => {
                assert.ok(last.content.includes(replacement));
                assert.ok(!last.content.includes(original));
                return answer(replacement);
            }
        ]);
        assert.ok(result.includes(replacement));
    }
    await run(cli, 'Prépare mon briefing.', [
        (_, payload) => {
            assert.ok(!JSON.stringify(payload).includes(replacement));
            return tool('lire_preference', {});
        },
        last => { assert.ok(!last.content.includes(replacement)); return answer('Aucune préférence.'); }
    ], randomUUID());
    console.log('OK : politique Acme, remplacement, rappel console et Teams, isolation entre comptes.');
} finally {
    server.close();
}
