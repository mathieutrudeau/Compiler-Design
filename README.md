# Compiler-Design
Compiler Design and Implementation.

Note that this Compiler was made to be simple (proof of concept) and as such is not optimized.
Furthermore, the compilation process turns the source code into Moon assembly code, which is then itself compiled using a C program.

## Tokenizer (Also called Scanner or Lexer)
A tokenizer, also known as a lexer (lexical analyzer), is the first phase of a compiler. Its main role is to take raw source code as input and convert it into a stream of tokens.

Each token represents a logically cohesive sequence of characters, such as an identifier, keyword, separator, operator, or literal. For example, in the code int x = 10;, the tokenizer would produce the tokens int, x, =, 10, and ;.

### Tokenizer Implementation Details

Each token can be represented by a Deterministic Finite Automaton (DFA) and as such can also be represented by its Regular Expression (RegEx) equivalent. The implementation defines a RegEx in order to identify any valid token. 



## Parser

A parser is the second phase of a compiler. It takes the stream of tokens produced by the tokenizer as input and transforms it into a parse tree, which is a tree representation of the syntactic structure of the source code.

The parser uses the grammar of the programming language to recognize and organize the tokens into a hierarchy that represents the program's structure. This structure follows the rules of the language's syntax and shows the relationships between different parts of the code.

For example, in an expression like a = b + c;, the parser would group b + c as one unit because the '+' operator has higher precedence than the '=' operator.

The parser used here is a predictive recursive descent parser which defines a method for each rule.

After the parsing phase, the parse tree is passed to the next stage of the compiler, typically the semantic analyzer, which checks the parse tree for semantic errors and annotates it with additional information needed for code generation.

## AST

An Abstract Syntax Tree (AST) is a tree representation of the abstract syntactic structure of source code. It is generated by the parser during the compilation process.

Unlike the parse tree, which represents every detail of the syntax of the program (including parentheses, semicolons, etc.), the AST abstracts away these details and focuses on the important information, such as operations, operands, and their relationships. For example, an if-statement in the source code would be represented as an "if" node in the AST with child nodes for the condition, the then-statement, and optionally the else-statement.

The AST is used in the semantic analysis phase of the compiler, where it is checked for semantic errors, and additional information (like data types or scope information) is added to the nodes. The AST is also used in the code generation phase, where it is traversed to produce the output code in the target language.

## Semantic Analyzer

The semantic analyzer is a phase of the compiler that takes the Abstract Syntax Tree (AST) produced by the parser and checks it for semantic errors. These are errors that are not related to syntax, but rather to the meaning of the program, such as type mismatches, undeclared variables, or improper use of operations.

The semantic analyzer also enriches the AST with additional information that is needed for the next phase of the compiler. This can include information about variable types, function signatures, and scope boundaries.

For example, in a statement like int x = y + z;, the semantic analyzer would check that y and z are declared and that they are of a type that can be added together and assigned to an int.

The output of the semantic analyzer is an annotated AST that is used in the code generation phase to produce the final machine code, as well as a symbol table used to generate frames to be added/removed from the runtime stack.

## Code Generation

Code generation is the final phase of a compiler. It takes the annotated Abstract Syntax Tree (AST) produced by the semantic analyzer and translates it into machine code or bytecode that can be executed by a machine or a virtual machine.

The code generator must take into account the specific architecture of the target machine, including its instruction set, register usage, calling conventions, and memory management.

The output of the code generation phase is a sequence of machine instructions that perform the operations specified by the source program. This output is typically written to an object file, which can then be linked with other object files to produce an executable program.

In this case, the output code is moon code which targets the moon virtual processor (Interpreter).
