# Compiler-Design
Compiler Design and Implementation for COMP 442 (Winter 2024) at Concordia University.


## Lexical Analyzer (Assignment 1)

The Lexical Analyzer (or Scanner) is used to tokenized the source code. It goes through the source code, character by character, and outputs the next token.

### Lexical Specifications

The following regular expressions were used to identify the lexical specification:

- nonzero: ^[1-9]$
- digit: ^[0-9]$
- letter: ^[a-zA-Z]$
- fraction: ^(\\.[0-9]*[1-9])|(\\.0)$
- alphanum: ^([a-zA-Z]|[0-9]|_)$
- id: ^([a-zA-Z])([a-zA-Z]|[0-9]|_)*$ 
- integer: ^(([1-9][0-9]*)|0)$
- float: ^(([1-9][0-9]*)|0)((\\.[0-9]*[1-9])|(\\.0))(e(\\+|-)??(([1-9][0-9]*)|0))??$
- operator: ^(==|=|<>|<|>|<=|>=|\\+|-|\\*|/|=|\\||&|!)$
- punctuation: ^(\\(|\\)|{|}|\\[|\\]|;|,|\\.|:|::|->)$
- reserved word: ^(if|then|else|integer|float|void|public|private|func|var|struct|while|read|write|return|self|inherits|let|impl)$
- inline comment: ^//.*$
- block comment (Each start must be matched with an end): 
    - start: ^/\\*(.|\\n)*$
    - end: .*\\*/$


The following regular expressions were used to identify lexical errors:

- invalid integer: ^0+((([1-9][0-9]*)|0))$
- invalid float: (^0+(([1-9][0-9]*)|0)((\\.[0-9]*[1-9])|(\\.0))(e(\\+|-)??(([1-9][0-9]*)|0))??$)|(^0*(([1-9][0-9]*)|0)((\\.[0-9]*[1-9])|(\\.0))(0+)(e(\\+|-)??(([1-9][0-9]*)|0))??$)|(^0*(([1-9][0-9]*)|0)((\\.[0-9]*[1-9])|(\\.0))(0*)(e(\\+|-)??(0+([1-9][0-9]*)|0))$)
- invalid id: ^([0-9]|_)([a-zA-Z]|[0-9]|_)*$

### Finite State Automaton

- integer: 


### Design

The design consists of a 2 classes: Scanner and Token.

The Scanner class has a public interface which allows for 3 methods to be used: NextToken() fetches and returns the next token, HasTokenLeft() returns true if a token has yet to be fetched and PrintBuffer() shows the content of the source file in the console.

The logic for NextToken() consists of reading the next character until a special character is found (either a whitespace, newline, indent or end-of-file (eof)). Once a special character is found, it check to see if the resulting string matches any final state (using regex). If it does, the string is returned as a token, otherwise it will backtrack until a final state is found (for tokens not seperated by whitespaces). 

Final states for comments are handled differently. For inline comment, it will read all characters until the either a newline or eof is found. For block comments, it will read until a matching amount of opening and closing tags are found or the eof is reached.

The Token class represents a single token, so it therefore contains Type, Lexeme and Location properties. All of the type handling/identification logic is handled inside the Token class using regex and enums.

### Tools Used

The implementation does not make use of any library outside of .NET 7's C# standard library. From the C# library, it makes use of the Regex class from the [RegularExpressions namespace](https://learn.microsoft.com/en-us/dotnet/api/system.text.regularexpressions?view=net-7.0) in order to define the regular expressions.