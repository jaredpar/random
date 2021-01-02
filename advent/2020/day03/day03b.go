package main

import (
	"bufio"
	"fmt"
	"os"
	"strings"
)

type grid struct {
	trees [][]bool
}

func (g *grid) IsTree(row int, column int) bool {
	rowLine := g.trees[row]
	column = column % len(rowLine)
	return rowLine[column]
}

func (g *grid) RowCount() int {
	return len(g.trees)
}

func (g *grid) String() string {
	var b strings.Builder
	for _, lineData := range g.trees {
		for _, t := range lineData {
			if t {
				b.WriteRune('#')
			} else {
				b.WriteRune('.')
			}
		}

		b.WriteRune('\n')
	}

	return b.String()
}

func check(e error) {
	if e != nil {
		panic(e)
	}
}

3316272960

func checkSlope(g *grid, rowIncrement int, columnIncrement int) int {
	treeCount := 0
	row := 0
	column := 0
	for {
		row += rowIncrement
		column += columnIncrement
		if row >= g.RowCount() {
			break
		}

		if g.IsTree(row, column) {
			treeCount++
		}
	}

	return treeCount
}

func main() {
	grid := ReadGrid()

	total := checkSlope(&grid, 1, 1) *
		checkSlope(&grid, 1, 3) *
		checkSlope(&grid, 1, 5) *
		checkSlope(&grid, 1, 7) *
		checkSlope(&grid, 2, 1)

	fmt.Printf("Hit %d trees\n", total)
}
