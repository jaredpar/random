package main

import (
	"bufio"
	"fmt"
	"io"
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

	// TODO: dynamically grow the buffer
	buffer := make([]int, 1000)
	index := 0
	reader := bufio.NewReader(file)

	for {
		line, _, err := reader.ReadLine()
		if err == io.EOF {
			break
		}

		s := string(line)
		n, err := strconv.Atoi(s)

		check(err)

		buffer[index] = n
		index = index + 1
	}

	file.Close()
	return buffer[0:index]
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
