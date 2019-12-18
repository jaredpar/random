"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
var lineReader = require("line-reader");
function getFuelForMass(mass) {
    mass = mass / 3;
    mass = Math.floor(mass);
    return mass - 2;
}
function getFuelForFuel(fuel) {
    var thisFuel = getFuelForMass(fuel);
    if (thisFuel > 0) {
        return thisFuel + getFuelForFuel(thisFuel);
    }
    return 0;
}
var total = 0;
lineReader.eachLine('input.txt', function (line, last) {
    var mass = parseInt(line);
    var fuel = getFuelForMass(mass);
    var fuel2 = fuel + getFuelForFuel(fuel);
    console.log(mass + " - " + fuel + " - " + fuel2);
    total += fuel2;
    if (last) {
        console.log("Total fuel " + total);
    }
});
//# sourceMappingURL=app.js.map