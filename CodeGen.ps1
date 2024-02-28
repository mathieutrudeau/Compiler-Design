# Description: This script will read a grammar file and generate a parser
# Note: The grammar (.grm) file must be a LL(1) grammar 
# Author: Mathieu Trudeau

$languageDict = @{}

<#
.SYNOPSIS
    Checks if a symbol is a terminal
.DESCRIPTION
    This function checks if a symbol is a terminal. A terminal is a symbol that is enclosed in single quotes.
.PARAMETER symbol
    The symbol to check
.EXAMPLE
    Is-Terminal -symbol "'id'"
.NOTES
    A terminal is a symbol that is enclosed in single quotes. For example, 'id' is a terminal.
#>
function Is-Terminal {
    param(
        [string]$symbol
    )

    return $symbol.Trim() -match "^'.*'$"
}

<#
.SYNOPSIS
    Checks if a symbol is a non-terminal
.DESCRIPTION
    This function checks if a symbol is a non-terminal. A non-terminal is a symbol that is enclosed in angle brackets.
.PARAMETER symbol
    The symbol to check
.EXAMPLE
    Is-NonTerminal -symbol "<START>"
.NOTES
    A non-terminal is a symbol that is enclosed in angle brackets. For example, <START> is a non-terminal.
#>
function Is-NonTerminal {
    param(
        [string]$symbol
    )

    return $symbol.Trim() -match "^<.*>$"
}

<#
.SYNOPSIS
    Extracts the name of a symbol
.DESCRIPTION
    This function extracts the name of a symbol. If the symbol is a non-terminal, it will remove the angle brackets and make the first letter uppercase.
    If the symbol is a terminal, it will remove the single quotes.
.PARAMETER symbol
    The symbol to extract the name from
.EXAMPLE
    Extract-Name -symbol "<START>"
.NOTES
    If the symbol is a non-terminal, it will remove the angle brackets and make the first letter uppercase.
    If the symbol is a terminal, it will remove the single quotes.  
#>
function Extract-Name {
    param(
        [string]$symbol
    )

    if (Is-NonTerminal -symbol $symbol) {
        $prodName = $symbol.Replace("<", "").Replace(">", "").Trim()
        $prodName = ($prodName.Substring(0, 1).ToUpper() + $prodName.Substring(1)).Trim()

        $prodName = $prodName.Replace("-", "_")

        if ($prodName -eq "START") {
            $prodName = "Start"
        }

        return $prodName
    }
    elseif (Is-Terminal -symbol $symbol) {
        return $symbol.Replace("'", "").Trim()
    }
    else {
        return $symbol.Trim()
    }
}

<#
.SYNOPSIS
    Translates a terminal to an enum value
.DESCRIPTION
    This function translates a terminal to an enum value. For example, 'id' will be translated to Id.
.PARAMETER terminal
    The terminal to translate
.EXAMPLE
    Get-Enum-Value -terminal "'id'
.NOTES
    This function translates a terminal to an enum value. For example, 'id' will be translated to Id.
#>
function Get-Enum-Value {
    param(
        [string]$terminal
    )

    $terminal = $terminal.Trim()

    switch ($terminal) {
        "EPSILON" { return "Epsilon" }
        "'id'" { return "Id" }
        "'intLit'" { return "Intnum" }
        "'intNum'" { return "Intnum" }
        "'floatLit'" { return "Floatnum" }
        "'+'" { return "Plus" }
        "'-'" { return "Minus" }
        "'or'" { return "Or" }
        "','" { return "Comma" }
        "'['" { return "Opensqbr" }
        "']'" { return "Closesqbr" }
        "'('" { return "Openpar" }
        "')'" { return "Closepar" }
        "'{'" { return "Opencubr" }
        "'}'" { return "Closecubr" }
        "';'" { return "Semi" }
        "':'" { return "Colon" }
        "'='" { return "Assign" }
        "'*'" { return "Mult" }
        "'/'" { return "Div" }
        "'not'" { return "Not" }
        "'->'" { return "Arrow" }
        "'func'" { return "Func" }
        "'.'" { return "Dot" }
        "'if'" { return "If" }
        "'else'" { return "Else" }
        "'impl'" { return "Impl" }
        "'and'" { return "And" }
        "'eq'" { return "Eq" }
        "'geq'" { return "Geq" }
        "'neq'" { return "Noteq" }
        "'lt'" { return "Lt" }
        "'leq'" { return "Leq" }
        "'gt'" { return "Gt" }
        "'void'" { return "TokenType.Void" }
        "'while'" { return "While" }
        "'return'" { return "Return" }
        "'read'" { return "TokenType.Read" }
        "'write'" { return "TokenType.Write" }
        "'then'" { return "Then" }
        "'struct'" { return "Struct" }
        "'inherits'" { return "Inherits" }
        "'integer'" { return "Integer" }
        "'float'" { return "Float" }
        "'let'" { return "Let" }
        "'public'" { return "Public" }
        "'private'" { return "Private" }
        "$" { return "Eof" }
        "'arrow'" { return "Arrow" }
        "'eof'" { return "Eof" }
        "'intlit'" { return "Intnum" }
        "'floatlit'" { return "Intnum" }
        Default {}
    }

    return $terminal
}


function Process-Production {
    param(
        [string]$production
    )

    # Split the production into the left and right side
    $sides = $production -split "::="
    $left = $sides[0].Trim()
    $right = $sides[1]

    # If the Non-terminal is not in the dictionary, add it
    if ($languageDict[$left].Count -eq 0) {
        $languageDict[$left] = @($right)
    }
    else {
        $languageDict[$left] += $right
    }   
}

function Show-Grammar {
    param(
        $grammarDict
    )

    foreach ($key in $grammarDict.Keys) {
        $rightSide = ""
        foreach ($value in $grammarDict[$key]) {
            if ($rightSide -eq "") {
                $rightSide = $value
            }
            else {
                $rightSide += " | " + $value
            }
        }
        Write-Host "$key -> $rightSide"
    }
}

function Generate-Production-Method {
    param(
        [string]$productionName,
        [System.Collections.ArrayList]$productions
    )

    Write-Host "Generating production method for $productionName"

    $productionName = $productionName.Trim()

    # Make the first letter of the production name uppercase
    $prodName = Extract-Name -symbol $productionName


    $methodCode = @"


    /// <summary>
    /// $prodName production rule
    /// </summary>
    /// <returns>True if the production rule is matched, false otherwise</returns>
    private bool $prodName() 
    {
        if(!SkipErrors(FIRST_$prodName, FOLLOW_$prodName))
            return false;
"@
        
    $hasEpsilon = $false
    $useIfElse = $false

    foreach ($prod in $productions) {
        $prod = $prod.Trim()

        #Write-Host "Production: $prod"
        
        $start = ($prod -split " ")[0].Trim()

        #Write-Host "Start: $start"
        
        if ($prod -eq "EPSILON") {
            $hasEpsilon = $true
            continue
        }
        
        if (Is-Terminal -symbol $start) {
            $translatedTerm = Get-Enum-Value -terminal $start

            $start = "$translatedTerm == LookAhead.Type"
        }
        else {
            $firstSet = (Generate-First-Set -productionName $start -grammarDict $languageDict)
            $start = Extract-Name -symbol $start
            $startAlt = "FIRST_$start.Contains(LookAhead.Type)"

            if($firstSet.Contains("EPSILON")) {
                $startAlt = "$startAlt || FOLLOW_$start.Contains(LookAhead.Type)"
            }

            $start = $startAlt
        }


        

        $elements = $prod.Split(' ', [System.StringSplitOptions]::RemoveEmptyEntries)

        $recMatch = ""
        $recOutput = "$productionName ->"
        
        foreach ($symbol in $elements) {
            if (Is-Terminal -symbol $symbol) {
                if ($recMatch -ne "") {
                    $recMatch += " && "
                }
                $recMatch += "Match(" + (Get-Enum-Value -terminal $symbol) + ")"
                $recOutput += " $symbol"
            }
            else {
                if ($recMatch -ne "") {
                    $recMatch += " && "
                }
                $recMatch += (Extract-Name -symbol $symbol) + "()"
                $recOutput += " $symbol"
            }
        }


        if ($useIfElse -eq $false) {
            $useIfElse = $true
            $methodCode += @"

        if ($start)
        {
            OutputDerivation("$recOutput");
            return $recMatch;
"@
        }
        else {
            $methodCode += @"

        else if ($start)
        {
            OutputDerivation("$recOutput");
            return $recMatch;
"@
        }

        

        $methodCode += @"

        }
"@

    }


    # Add the epsilon production to the method if it exists

    if ($hasEpsilon -eq $true) {
        if ($useIfElse -eq $true) {
            $methodCode += @"

        else if (FOLLOW_$prodName.Contains(LookAhead.Type))
        {
            OutputDerivation("$productionName -> EPSILON");
            return true;
        }
"@                
        }
        else {
            $methodCode += @"

        if (FOLLOW_$prodName.Contains(LookAhead.Type))
        {
            OutputDerivation("$productionName -> EPSILON");
            return true;
        }
"@  
        }
    }

    $methodCode += @"

        else
            return false;
    }
"@
            
    return $methodCode
}

function Generate-Set-Code {
    param(
        [string]$productionName,
        $set,
        [bool]$isFollow = $false
    )
    $prodName = Extract-Name -symbol $productionName

    $setType = "FIRST"

    if ($isFollow -eq $true) {
        $setType = "FOLLOW"
    }

    $prodName = $setType + "_" + $prodName

    return @"

    private static readonly TokenType[] $prodName = new TokenType[] { $(($set | ForEach-Object { Get-Enum-Value -terminal $_ }) -join ", ") };
"@
}

<#
.SYNOPSIS
    Generates the first set for a production rule
.DESCRIPTION
    This function generates the first set for a production rule.
    It does so by checking if the first element of the production is a terminal. If it is, it adds it to the first set.
    If the first element is a non-terminal, it adds the elements of the first set of the non-terminal to the first set of the current production.
    If the first element is epsilon, it adds it to the first set.
.PARAMETER productionName
    The name of the production rule
.PARAMETER grammarDict
    The grammar dictionary
.PARAMETER firstSet
    The first set
.EXAMPLE
    Generate-First-Set-Rec -productionName "<START>" -grammarDict $languageDict -firstSet $firstSet
.NOTES
    This function is called recursively to generate the first set for a production rule.
#>
function Generate-First-Set-Rec {
    param(
        [string]$productionName,
        [System.Collections.Hashtable]$grammarDict,
        [System.Collections.ArrayList]$firstSet
    )

    # Getting all productions for the given LHS symbol
    $productions = $grammarDict[$productionName]

    # Loop through each production
    foreach ($prod in $productions) {

        #Write-Host "Production: $prod"
        
        # Split the production into elements
        $elements = ($prod.Trim() -split " ")
        $first = $elements[0].Trim()
     
        #Write-Host "First element: $first"

        # If the first element is a terminal, add it to the first set if it's not already there
        if ((Is-Terminal -symbol $first) -and (!$firstSet.Contains($first))) {
            #Write-Host "Adding $first to first set"
            $firstSet.Add($first) | Out-Null
        }
        # If the first element is a non-terminal, add the elements of the first set of the non-terminal to the first set of the current production
        elseif (Is-NonTerminal -symbol $first) {

            # Get the first set of the non-terminal
            $nt_firstSet = (Generate-First-Set-Rec -productionName $first -grammarDict $grammarDict -firstSet $firstSet)

            # Add the elements of the first set of the non-terminal to the first set of the current production
            $nt_firstSet | ForEach-Object {
                if (!$firstSet.Contains($_)) {
                    #Write-Host "Adding $_ to first set"
                    $firstSet.Add($_) | Out-Null
                }
            }
        }
        # If the first element is epsilon, add it to the first set if it's not already there
        elseif ($first -eq "EPSILON" -and !$firstSet.Contains("EPSILON")) {
            $firstSet.Add("EPSILON") | Out-Null
        }
    }

    return $firstSet
}

<#
.SYNOPSIS
    Generates the first set for a production rule
.DESCRIPTION
    This function generates the first set for a production rule.
.PARAMETER productionName
    The name of the production rule
.PARAMETER grammarDict
    The grammar dictionary
.EXAMPLE
    Generate-First-Set -productionName "<START>" -grammarDict $languageDict
#>
function Generate-First-Set {
    param(
        [string]$productionName,
        [System.Collections.Hashtable]$grammarDict
    )

    Write-Host "Generating first set for $productionName"

    # Create an array list to store the first set
    $firstSet = New-Object System.Collections.ArrayList

    # Call the recursive function to generate the first set
    $firstSet = (Generate-First-Set-Rec -productionName $productionName -grammarDict $grammarDict -firstSet $firstSet)

    # Return the first set
    return $firstSet
}

function Generate-Follow-Set-Rec {
    param(
        [string]$productionName,
        [System.Collections.Hashtable]$grammarDict,
        [System.Collections.ArrayList]$followSet,
        [System.Collections.ArrayList]$visited
    )

    # If this production has already been visited, return the follow set to avoid infinite recursion
    if ($visited.Contains($productionName)) {
        return $followSet
    }
    # Otherwise, add the production to the visited list
    else {
        $visited.Add($productionName) | Out-Null
    }

    # Iterate through each LHS symbol in the grammar dictionary
    foreach ($key in $grammarDict.Keys) {
        
        # Get all productions for the current LHS symbol
        $productions = $grammarDict[$key]

        # If the current LHS symbol is the same as the production name, skip it. 
        # We can't find the follow set of a production in itself
        if ($key -eq $productionName) {
            continue
        }

        # Iterate through each RHS symbol in the production
        foreach ($prod in $productions) {

            # If the RHS contains the production name, this production is a candidate for the follow set,
            # Otherwise, skip it
            if ($prod.Contains($productionName)) {

                #Write-Host "Found $productionName in $key"
                #Write-Host "Production: $key -> $prod"
                
                # Split the production into elements
                $elements = $prod.Split(' ', [System.StringSplitOptions]::RemoveEmptyEntries)
                
                # Get the count of elements (minus 1 because the array is 0-based)
                $count = $elements.Count - 1

                # Iterate through each element in the production
                for ($i = 0; $i -le $count; $i++) {

                    # If the current element is the production name, 
                    # then the next element(if it has one) is a candidate for the follow set
                    if ($elements[$i] -eq $productionName) {
                        #Write-Host "Element: $($elements[$i])"
                        
                        # If the element is not the last element in the production,
                        # then the next element is a candidate for the follow set
                        if ($i -lt $count) {

                            # Get the next element
                            $next = $elements[$i + 1]

                            # If the next element is a terminal symbol that is not already in the follow set, add it to the follow set
                            if ((Is-Terminal -symbol $next) -and !$followSet.Contains($next)) {
                                #Write-Host "Adding $next to follow set"
                                $followSet.Add($next) | Out-Null
                            }
                            # If the next element is a non-terminal symbol, add the first set of the non-terminal to the follow set
                            elseif (Is-NonTerminal -symbol $next) {

                                # Get the first set of the next non-terminal symbol
                                $nt_firstSet = (Generate-First-Set -productionName $next -grammarDict $grammarDict)

                                # Add the elements of the first set of the next non-terminal symbol to the follow set,
                                # except for epsilon of course
                                $nt_firstSet | ForEach-Object {
                                    if (!$followSet.Contains($_) -and ($_ -ne "EPSILON")) {
                                        #Write-Host "Adding $_ to follow set"
                                        $followSet.Add($_) | Out-Null
                                    }
                                }

                                # If the first set of the next non-terminal symbol contains epsilon,
                                # then the follow set of that non-terminal symbol needs to be added to the follow set of the current production 
                                if ($nt_firstSet.Contains("EPSILON")) {

                                    # Get the follow set of the next non-terminal symbol
                                    $nextFollow = (Generate-Follow-Set-Rec -productionName $next -grammarDict $grammarDict -followSet $followSet -visited $visited)

                                    # Add the elements of the follow set of the next non-terminal symbol to the follow set of the current production
                                    $nextFollow | ForEach-Object {
                                        if (!$followSet.Contains($_)) {
                                            #Write-Host "Adding $_ to follow set"
                                            $followSet.Add($_) | Out-Null
                                        }
                                    }
                                }
                            }
                            

                            #Write-Host "Next: $next"
                        }
                        # If the element is the last element in the production,
                        # then the follow set of the LHS symbol is a candidate for the follow set
                        else {
                            #Write-Host "Next: End of production"

                            # Get the follow set of the LHS symbol
                            $nextFollow = (Generate-Follow-Set-Rec -productionName $key -grammarDict $grammarDict -followSet $followSet -visited $visited)

                            # Add the elements of the follow set of the LHS symbol to the follow set of the current production
                            $nextFollow | ForEach-Object {
                                if (!$followSet.Contains($_)) {
                                    #Write-Host "Adding $_ to follow set"
                                    $followSet.Add($_) | Out-Null
                                }
                            }
                        }
                    }
                    
                }
            }
        }
    }

    # Return the follow set of the current production
    return $followSet
}

function Generate-Follow-Set {
    param(
        [string]$productionName,
        [System.Collections.Hashtable]$grammarDict
    )

    if ($productionName -eq "<factor>") {
        Write-Host "Generating follow set for $productionName"
    }

    $followSet = New-Object System.Collections.ArrayList
    $visited = New-Object System.Collections.ArrayList

    Write-Host "Generating follow set for $productionName"

    if ($productionName -eq "<START>") {
        $followSet.Add("$") | Out-Null
    }

    # SANITY CHECKS

    # Do a quick check to see if the production is ambiguous,
    # It can be ambiguous if the production has many derivations and the first set has overlapping elements with the follow set
    $followSet = (Generate-Follow-Set-Rec -productionName $productionName -grammarDict $grammarDict -followSet $followSet -visited $visited)
    $firstSet = (Generate-First-Set -productionName $productionName -grammarDict $grammarDict)
    $elementCount = $grammarDict[$productionName].Count

    if ($elementCount -gt 1 -and $firstSet.Contains("EPSILON")) {
        $firstSet | ForEach-Object {
            if ($followSet.Contains($_) ) {
                Write-Error "Error: $productionName : $_ is in the first set and the follow set"
            }
        }
    }

    return $followSet
}

function Generate-CSharp-Parser-Code {
    param(
        [System.Collections.Hashtable]$grammarDict
    )

    $code = @"
using LexicalAnalyzer;
using static System.Console;
using static LexicalAnalyzer.TokenType;
    
namespace SyntacticAnalyzer;

/// <summary>
/// Parser class, used to parse a source file.
/// </summary>
public class Parser : IParser
{
    /// <summary>
    /// The scanner used to scan and tokenize the source file
    /// </summary>
    private IScanner Scanner { get; }

    /// <summary>
    /// The current token being looked at
    /// </summary>
    private Token LookAhead { get; set; }

    /// <summary>
    /// The source file to parse (with the file extension)
    /// </summary>
    private string Source {get;} = "";

    /// <summary>
    /// The name of the source file to parse
    /// </summary>
    private string SourceName {get;} = "";
    
    /// <summary>
    /// The parse list used to track the derivations
    /// </summary>
    private IParseList ParseList { get; set; } = new ParseList();

    #region Constants

    /// <summary>
    /// Extension for the output syntax errors file
    /// </summary>
    private const string OUT_SYNTAX_ERRORS_EXTENSION = ".outsyntaxerrors";

    /// <summary>
    /// Extension for the output derivation file
    /// </summary>
    private const string OUT_DERIVATION_EXTENSION = ".outderivation";

    /// <summary>
    /// Extension for the output productions file
    /// </summary>
    private const string OUT_PRODUCTIONS_EXTENSION = ".outproductions";

    #endregion Constants

    #region Constructor

    /// <summary>
    /// Constructor for the Parser class
    /// </summary>
    /// <param name="sourceFileName">The name of the source file to parse</param>
    public Parser(string sourceFileName)
    {
        // Create a new scanner and get the first token
        Scanner = new Scanner(sourceFileName);
        LookAhead = new Token();

        // Set the source file 
        Source = sourceFileName;
        SourceName = sourceFileName.Replace(".src", "");

        // Delete the output files if they exist
        if (File.Exists(SourceName + OUT_SYNTAX_ERRORS_EXTENSION))
            File.Delete(SourceName + OUT_SYNTAX_ERRORS_EXTENSION);
        if (File.Exists(SourceName + OUT_DERIVATION_EXTENSION))
            File.Delete(SourceName + OUT_DERIVATION_EXTENSION);
        if (File.Exists(SourceName + OUT_PRODUCTIONS_EXTENSION))
            File.Delete(SourceName + OUT_PRODUCTIONS_EXTENSION);
    }

    #endregion Constructor

    #region Base Methods

    /// <summary>
    /// Parses the source file, returning true if the source file is parsed successfully, false otherwise
    /// </summary>
    /// <returns>True if the source file is parsed successfully, false otherwise</returns>
    public bool Parse()
    {
        // Get the next token and make sure it is not a Comment
        do
            LookAhead = Scanner.NextToken();
        while (LookAhead.Type == Blockcmt || LookAhead.Type == Inlinecmt);

        // Output the derivation to the output file
        using StreamWriter sw = new(SourceName + OUT_DERIVATION_EXTENSION, true);
        sw.WriteLine(ParseList.GetDerivation());
        sw.Close();
        
        // Parse the source file   
        return Start();
    }

    /// <summary>
    /// Matches the current token with the expected token
    /// </summary>
    /// <param name="tokenType">The expected token type</param>
    /// <returns>True if the current token matches the expected token, false otherwise</returns>
    private bool Match(TokenType tokenType)
    {
        if (tokenType == Eof)
            while (LookAhead.Type == Blockcmt || LookAhead.Type == Inlinecmt)
                LookAhead = Scanner.NextToken();

        // Check if the current token matches the expected token
        bool isMatch = LookAhead.Type == tokenType;

        if (!isMatch)
        {
            string errorMsg = $"Syntax error: Unexpected '{LookAhead.Lexeme}' at line {LookAhead.Location}. Expected {Token.TokenTypeToString(tokenType)}.";
            WriteLine(errorMsg);
            using StreamWriter sw = new(SourceName + OUT_SYNTAX_ERRORS_EXTENSION, true);
            sw.WriteLine(errorMsg);
        }

        // Get the next token and make sure it is not a Comment
        do
            LookAhead = Scanner.NextToken();
        while (LookAhead.Type == Blockcmt || LookAhead.Type == Inlinecmt);

        // Return the result
        return isMatch;
    }

    /// <summary>
    /// Skips errors in the source file
    /// </summary>
    /// <param name="firstSet">The first set of the production rule</param>
    /// <param name="followSet">The follow set of the production rule</param>
    /// <returns>True if there are no errors (or if the error is recovered from), false otherwise</returns>
    private bool SkipErrors(TokenType[] firstSet, TokenType[] followSet)
    {
        // Check if the current token is in the first set or the follow set (if it is an epsilon token)
        if (firstSet.Contains(LookAhead.Type) || (firstSet.Contains(Epsilon) && followSet.Contains(LookAhead.Type)))
            return true;
        else
        {
            // Output an error message, this error can be recovered from
            string[] expectedTokens = firstSet.Contains(Epsilon) ? 
            firstSet.Concat(followSet).Distinct().Where(x => x != Epsilon && x!=Eof).Select(Token.TokenTypeToString).ToArray() :
            firstSet.Distinct().Where(x => x != Epsilon && x!=Eof).Select(Token.TokenTypeToString).ToArray();

            // Define the error message
            string errorMsg = $"Syntax error: Unexpected token '{LookAhead.Lexeme}' at line {LookAhead.Location}. Expected any of the following: {string.Join(", ", expectedTokens)}.";

            // Write the error message to the console and to the output file
            WriteLine(errorMsg);
        
            using StreamWriter sw = new(SourceName + OUT_SYNTAX_ERRORS_EXTENSION, true);
            sw.WriteLine(errorMsg);

            // Get the next token until it is in the first set or the follow set
            while (!firstSet.Contains(LookAhead.Type) && !followSet.Contains(LookAhead.Type))
            {
                // Get the next token and make sure it is not a Comment
                do
                    LookAhead = Scanner.NextToken();
                while (LookAhead.Type == Blockcmt || LookAhead.Type == Inlinecmt);

                // If the end of the file is reached, return false
                if(LookAhead.Type == Eof)
                    return false;

                // If the current token contains epsilon and the follow set contains the current token, return false
                if (firstSet.Contains(Epsilon) && followSet.Contains(LookAhead.Type))
                    return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Outputs a derivation to the console and to the output file
    /// </summary>
    /// <param name="productionRuleStr">The production rule to output</param>
    private void OutputDerivation(string productionRuleStr)
    {
        // Add the production rule to the parse list
        ParseList.Add(productionRuleStr);

        // Write the derivation to the output file
        using StreamWriter sw = new(SourceName + OUT_DERIVATION_EXTENSION, true);
        sw.WriteLine(ParseList.GetDerivation());

        // Write the production rule to the output file
        using StreamWriter sw1 = new(SourceName + OUT_PRODUCTIONS_EXTENSION, true);
        sw1.WriteLine(productionRuleStr);
    }

    #endregion Base Methods

    #region First Sets
    $($grammarDict.Keys | Sort-Object | ForEach-Object { Generate-Set-Code -productionName $_ -set (Generate-First-Set -productionName $_ -grammarDict $grammarDict) })

    #endregion First Sets

    #region Follow Sets
    $($grammarDict.Keys | Sort-Object | ForEach-Object { Generate-Set-Code -productionName $_ -set (Generate-Follow-Set -productionName $_ -grammarDict $grammarDict) -isFollow $true })

    #endregion Follow Sets

    #region Productions
    $($grammarDict.Keys | Sort-Object | ForEach-Object { Generate-Production-Method -productionName $_ -productions $grammarDict[$_] })

    #endregion Productions
}
"@

    return $code
    
}



$grammar = Get-Content -Path "fixedgrammar.grm"

# Loop through each line in the grammar file and process the production
foreach ($line in $grammar) {
    if ($line -eq "") {
        continue
    }

    if ($line[0] -eq "#") {
        continue
    }
    
    Process-Production -production $line
}


# Create the parser code
Generate-CSharp-Parser-Code -grammarDict $languageDict | Out-File -FilePath "SyntacticAnalyzer/Parser.cs" -Encoding utf8
