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

func ReadGrid() grid {
	var trees [][]bool

	file, err := os.Open("map.txt")
	check(err)
	reader := bufio.NewReader(file)
	scanner := bufio.NewScanner(reader)

	for {
		if !scanner.Scan() {
			break
		}

		line := scanner.Text()
		lineData := make([]bool, 0, len(line))
		for _, r := range line {
			c := false
			if r == '#' {
				c = true
			}
			lineData = append(lineData, c)
		}

		trees = append(trees, lineData)
	}

	return grid{trees: trees}
}

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
