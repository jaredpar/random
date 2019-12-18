import * as lineReader from 'line-reader'

function getFuelForMass(mass: number): number {
    mass = mass / 3
    mass = Math.floor(mass)
    return mass - 2
}

function getFuelForFuel(fuel: number): number {
    let thisFuel = getFuelForMass(fuel);
    if (thisFuel > 0) {
        return thisFuel + getFuelForFuel(thisFuel)
    }

    return 0
}

let total = 0
lineReader.eachLine('input.txt', function(line: string, last: boolean) {
    let mass = parseInt(line);
    let fuel = getFuelForMass(mass);
    let fuel2 = fuel + getFuelForFuel(fuel)
    console.log(`${mass} - ${fuel} - ${fuel2}`);
    total += fuel2
    if (last) {
        console.log(`Total fuel ${total}`)
    }
});
