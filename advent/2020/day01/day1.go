package main

import (
	"bufio"
	"fmt"
	"os"
	"strconv"
)

func check(e error) {
	if e != nil {
		panic(e)
	}
}

func ReadInts() []int {
	file, err := os.Open("report.txt")
	check(err)

	var buffer []int
	reader := bufio.NewReader(file)
	scanner := bufio.NewScanner(reader)

	for {
		if !scanner.Scan() {
			break
		}

		line := scanner.Text()
		n, err := strconv.Atoi(line)

		check(err)
		buffer = append(buffer, n)
	}

	file.Close()
	return buffer
}

func part1() {
	buffer := ReadInts()
	for i := 0; i < len(buffer); i++ {
		for j := 0; j < len(buffer); j++ {
			if buffer[i]+buffer[j] == 2020 {
				fmt.Println(buffer[i] * buffer[j])
			}
		}
	}
}

func part2() {
	buffer := ReadInts()
	fmt.Printf("Read %v elements\n", len(buffer))
	for i := 0; i < len(buffer); i++ {
		for j := 0; j < len(buffer); j++ {
			for k := 0; k < len(buffer); k++ {
				if buffer[i]+buffer[j]+buffer[k] == 2020 {
					fmt.Println(buffer[i] * buffer[j] * buffer[k])
				}
			}
		}
	}
}

func main() {
	part2()
}
