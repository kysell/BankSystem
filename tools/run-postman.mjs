// Small dependency-free runner for this collection's exact pm API subset. This is not Newman/Postman.
import fs from 'node:fs/promises';
import vm from 'node:vm';
import assert from 'node:assert/strict';
import { randomUUID, randomInt } from 'node:crypto';
const collection = JSON.parse(await fs.readFile(new URL('../postman/BankSystem.postman_collection.json', import.meta.url)));
const environment = JSON.parse(await fs.readFile(new URL('../postman/BankSystem.postman_environment.json', import.meta.url)));
const values = new Map(environment.values.map(v => [v.key, v.value]));
if (process.argv[2]) values.set('baseUrl', process.argv[2]);
let requests = 0, tests = 0;
function interpolate(value) {
  return value.replace(/\{\{([^}]+)\}\}/g, (_, key) => {
    if (key === '$guid') return randomUUID();
    if (key === '$randomInt') return String(randomInt(100000, 999999));
    if (key === '$randomFirstName') return `Student${randomInt(1000, 9999)}`;
    assert(values.has(key), `Missing variable: ${key}`);
    return values.get(key);
  });
}
async function run(items) {
  for (const item of items) {
    if (item.item) { await run(item.item); continue; }
    const req = item.request;
    const res = await fetch(interpolate(req.url), {method:req.method,
      headers:Object.fromEntries((req.header ?? []).map(h => [h.key, interpolate(h.value)])),
      body:req.body ? interpolate(req.body.raw) : undefined, signal:AbortSignal.timeout(10000)});
    const text = await res.text(); requests++;
    const pm = {response:{code:res.status,json:()=>JSON.parse(text),to:{have:{status:expected=>assert.equal(res.status,expected,`${item.name}: ${text}`)}}},
      environment:{set:(key,value)=>values.set(key,value)},
      test:(name,fn)=>{fn();tests++;console.log(`  PASS ${name}`);},
      expect:value=>({to:{eql:expected=>assert.deepEqual(value,expected)}})};
    for (const event of item.event ?? []) if (event.listen === 'test') vm.runInNewContext(event.script.exec.join('\n'), {pm,console}, {timeout:1000});
  }
}
await run(collection.item);
console.log(`Passed: ${requests} HTTP requests, ${tests} assertions`);
