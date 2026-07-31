/* eslint-disable */
const path = require("path");
const { defineConfigs } = require("../Rock.JavaScript.Obsidian/Build/build-tools");

const workspacePath = path.resolve(__dirname);
const srcPath = path.join(workspacePath, "src");
const outPath = path.join(workspacePath, "dist");
const pluginOutPath = path.join(workspacePath, "..", "RockWeb", "Plugins", "com_yesuchum", "RockDetective");

module.exports = [
    ...defineConfigs(srcPath, outPath, {
        copy: pluginOutPath
    })
];
