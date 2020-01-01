import * as fs from 'fs'

function process(instructions: number[]): number {
    let index = 0
    while (true) {
        let instruction = instructions[index];
        if (instruction === 99) {
            break;
        }

        let leftIndex = instructions[index + 1]
        let rightIndex = instructions[index + 2]
        let left = instructions[leftIndex]
        let right = instructions[rightIndex]
        let destIndex = instructions[index + 3]
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

function convertToInstructions(contents: string): number[] {
    let instructions = contents.split(',').map(c => parseInt(c));
    return instructions;
}

function processText(contents: string) {
    let instructions = convertToInstructions(contents);
    let value = process(instructions);
    console.log(value);
}

function processFile() {
    fs.readFile('input.txt', 'utf8', (err, contents: string) => processText(contents));
}

function processSolver() {
    function processInstructions(instructions: number[]): [number, number] | null {
        for (let i = 0; i <= 99; i++) {
            for (let j = 0; j <= 99; j++) {
                const copy = [...instructions];
                copy[1] = i;
                copy[2] = j;
                const value = process(copy);
                if (value === 19690720) {
                    return [i, j];
                }
            }
        }

        return null;
    }

    fs.readFile('input.txt', 'utf8', function (err, contents: string) {
        let instructions = convertToInstructions(contents);
        let result = processInstructions(instructions);
        if (result) {
            let [noun, verb] = result
            console.log(`noun: ${noun} verb: ${verb}`);
            let answer = (noun * 100) + verb;
            console.log(answer);
        }
        else {
            console.log("Could not determine the values");
        }
    });
}

processSolver();
// processFile();
