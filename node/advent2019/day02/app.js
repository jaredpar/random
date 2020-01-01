"use strict";
var __spreadArrays = (this && this.__spreadArrays) || function () {
    for (var s = 0, i = 0, il = arguments.length; i < il; i++) s += arguments[i].length;
    for (var r = Array(s), k = 0, i = 0; i < il; i++)
        for (var a = arguments[i], j = 0, jl = a.length; j < jl; j++, k++)
            r[k] = a[j];
    return r;
};
Object.defineProperty(exports, "__esModule", { value: true });
var fs = require("fs");
function process(instructions) {
    var index = 0;
    while (true) {
        var instruction = instructions[index];
        if (instruction === 99) {
            break;
        }
        var leftIndex = instructions[index + 1];
        var rightIndex = instructions[index + 2];
        var left = instructions[leftIndex];
        var right = instructions[rightIndex];
        var destIndex = instructions[index + 3];
        if (instruction == 1) {
            // console.log(`Add ${leftIndex}(${left}) + ${rightIndex}(${right}) into ${destIndex}`)
            instructions[destIndex] = left + right;
        }
        else {
            // console.log(`Multiply ${leftIndex}(${left}) + ${rightIndex}(${right}) into ${destIndex}`)
            instructions[destIndex] = left * right;
        }
        index += 4;
    }
    return instructions[0];
}
function convertToInstructions(contents) {
    var instructions = contents.split(',').map(function (c) { return parseInt(c); });
    return instructions;
}
function processText(contents) {
    var instructions = convertToInstructions(contents);
    var value = process(instructions);
    console.log(value);
}
function processFile() {
    fs.readFile('input.txt', 'utf8', function (err, contents) { return processText(contents); });
}
function processSolver() {
    function processInstructions(instructions) {
        for (var i = 0; i <= 99; i++) {
            for (var j = 0; j <= 99; j++) {
                var copy = __spreadArrays(instructions);
                copy[1] = i;
                copy[2] = j;
                var value = process(copy);
                if (value === 19690720) {
                    return [i, j];
                }
            }
        }
        return null;
    }
    fs.readFile('input.txt', 'utf8', function (err, contents) {
        var instructions = convertToInstructions(contents);
        var result = processInstructions(instructions);
        if (result) {
            var noun = result[0], verb = result[1];
            console.log("noun: " + noun + " verb: " + verb);
            var answer = (noun * 100) + verb;
            console.log(answer);
        }
        else {
            console.log("Could not determine the values");
        }
    });
}
processSolver();
// processFile();
//# sourceMappingURL=app.js.map