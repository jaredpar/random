package main

import (
	"bufio"
	"fmt"
	"os"
	"regexp"
	"strconv"
)

func check(e error) {
	if e != nil {
		panic(e)
	}
}

type entry struct {
	min      int
	max      int
	target   rune
	password string
}

func parseFile() []entry {
	file, err := os.Open("report.txt")
	check(err)
	reader := bufio.NewReader(file)
	scanner := bufio.NewScanner(reader)

	r, err := regexp.Compile("([0-9]+)-([0-9]+) ([a-z]): ([a-z]+)")
	check(err)
	var entries []entry
	for {
		if !scanner.Scan() {
			break
		}

		line := scanner.Text()
		items := r.FindStringSubmatch(line)
		min, _ := strconv.Atoi(items[1])
		max, _ := strconv.Atoi(items[2])
		var target rune = rune(items[3][0])

		entry := entry{min: min, max: max, target: target, password: items[4]}
		entries = append(entries, entry)
	}

	return entries
}

func filter(entries []entry) []entry {
	var valid []entry
	for _, e := range entries {
		count := 0
		for _, r := range e.password {
			if r == e.target {
				count++
			}
		}

		if count >= e.min && count <= e.max {
			valid = append(valid, e)
		}
	}

	return valid
}

func printEntries(entries []entry) {
	for _, e := range entries {
		fmt.Printf("%d-%d %v %v\n", e.min, e.max, e.target, e.password)
	}
}

func main() {
	entries := parseFile()
	valid := filter(entries)
	fmt.Println(len(valid))

}
