"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
var fs = require("fs");
function process(contents) {
    var instructions = contents.split(',').map(function (c) { return parseInt(c); });
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
            console.log("Add " + leftIndex + "(" + left + ") + " + rightIndex + "(" + right + ") into " + destIndex);
            instructions[destIndex] = left + right;
        }
        else {
            console.log("Multiply " + leftIndex + "(" + left + ") + " + rightIndex + "(" + right + ") into " + destIndex);
            instructions[destIndex] = left * right;
        }
        index += 4;
    }
    console.log(instructions[0]);
}
fs.readFile('input.txt', 'utf8', function (err, contents) { return process(contents); });
//# sourceMappingURL=app.js.map