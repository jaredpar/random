import * as fs from 'fs'

function process(contents: string) {
    let instructions = contents.split(',').map(c => parseInt(c));
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
            console.log(`Add ${leftIndex}(${left}) + ${rightIndex}(${right}) into ${destIndex}`)
            instructions[destIndex] = left + right;
        }
        else {
            console.log(`Multiply ${leftIndex}(${left}) + ${rightIndex}(${right}) into ${destIndex}`)
            instructions[destIndex] = left * right;
        }
        index += 4;
    }

    console.log(instructions[0]);
}

fs.readFile('input.txt', 'utf8', (err, contents: string) => process(contents));