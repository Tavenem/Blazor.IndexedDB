import * as esbuild from 'esbuild';

const args = process.argv.slice(2);

if (!args.length
    || args.length < 2) {
    console.log("Missing esbuild args");
    process.exit(1);
}
if (args[0] !== '-o') {
    console.log("Missing esbuild arg -o");
    process.exit(1);
}
if (!args[1] || !args[1].length) {
    console.log("Missing esbuild arg -o <outdir>");
    process.exit(1);
}

await esbuild.build({
    entryPoints: [
        "tavenem-indexeddb.ts",
    ],
    bundle: true,
    format: 'esm',
    minify: true,
    outdir: args[1],
    sourcemap: true,
});