package main

import (
	"bufio"
	"fmt"
	"os"
	"sort"
	"strings"
)

type passport struct {
	data map[string]string
}

func (p *passport) String() string {
	keys := make([]string, 0, len(p.data))
	for k := range p.data {
		keys = append(keys, k)
	}

	sort.Strings(keys)
	var b strings.Builder
	for _, k := range keys {
		fmt.Fprintf(&b, "%v:%v", k, p.data[k])
	}

	return b.String()
}

func check(e error) {
	if e != nil {
		panic(e)
	}
}

func ReadPassports() []passport {
	var passports []passport

	file, err := os.Open("data.txt")
	check(err)
	reader := bufio.NewReader(file)
	scanner := bufio.NewScanner(reader)

	for {
		if !scanner.Scan() {
			break
		}

		data := make(map[string]string)
		line := scanner.Text()
		for _, entry := range strings.Split(line, " ") {
			if len(entry) > 0 {
				both := strings.Split(entry, ":")
				data[both[0]] = both[1]
			}
		}

		passport := passport{data: data}
		passports = append(passports, passport)
	}

	return passports
}

func main() {
	passports := ReadPassports()
	for _, p := range passports {
		fmt.Println(p.String())
	}
}
