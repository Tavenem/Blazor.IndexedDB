import typescript from '@rollup/plugin-typescript';
import { nodeResolve } from '@rollup/plugin-node-resolve';
import { terser } from 'rollup-plugin-terser';

let plugins = [
    typescript(),
    nodeResolve({
        mainFields: ['module', 'main'],
        extensions: '.ts'
    }),
];
if (process.env.build === 'Release') {
    plugins.push(terser());
}

export default [{
    input: "./tavenem-indexeddb.ts",
    output: {
        format: 'es',
        sourcemap: true,
    },
    plugins: plugins,
}];