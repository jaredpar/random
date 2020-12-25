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

func main() {
	grid := ReadGrid()

	treeCount := 0
	row := 0
	column := 0
	for {
		row += 1
		column += 3
		if row >= grid.RowCount() {
			break
		}

		if grid.IsTree(row, column) {
			treeCount++
		}
	}

	fmt.Printf("Hit %d trees\n", treeCount)
	// fmt.Println(grid.String())
}
