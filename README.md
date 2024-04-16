# Compiler-Design
Compiler Design and Implementation for COMP 442 (Winter 2024) at Concordia University.


## Tokenizer
A tokenizer, also known as a lexer (lexical analyzer), is the first phase of a compiler. Its main role is to take raw source code as input and convert it into a stream of tokens.

Each token represents a logically cohesive sequence of characters, such as an identifier, keyword, separator, operator, or literal. For example, in the code int x = 10;, the tokenizer would produce the tokens int, x, =, 10, and ;.

## Parser

A parser is the second phase of a compiler. It takes the stream of tokens produced by the tokenizer as input and transforms it into a parse tree, which is a tree representation of the syntactic structure of the source code.

The parser uses the grammar of the programming language to recognize and organize the tokens into a hierarchy that represents the program's structure. This structure follows the rules of the language's syntax and shows the relationships between different parts of the code.

For example, in an expression like a = b + c;, the parser would group b + c as one unit because the '+' operator has higher precedence than the '=' operator.

There are different types of parsers, such as top-down parsers (which start from the root of the parse tree and work their way down) and bottom-up parsers (which start from the leaves and work their way up). The choice of parser type depends on the specific requirements of the programming language.

After the parsing phase, the parse tree is passed to the next stage of the compiler, typically the semantic analyzer, which checks the parse tree for semantic errors and annotates it with additional information needed for code generation.

## AST

## Semantic Analyzer

## Code Generation
