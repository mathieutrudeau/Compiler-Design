# Compiler-Design
Compiler Design and Implementation for COMP 442 (Winter 2024) at Concordia University.


## Tokenizer
A tokenizer, also known as a lexer (lexical analyzer), is the first phase of a compiler. Its main role is to take raw source code as input and convert it into a stream of tokens.

Each token represents a logically cohesive sequence of characters, such as an identifier, keyword, separator, operator, or literal. For example, in the code int x = 10;, the tokenizer would produce the tokens int, x, =, 10, and ;.

The tokenizer also typically removes whitespace and comments from the source code, as they are not needed for further stages of compilation.

These tokens are then passed to the next stage of the compiler, the parser, which uses them to construct a parse tree representing the syntactic structure of the program.

## Parser

## AST

## Semantic Analyzer

## Code Generation
