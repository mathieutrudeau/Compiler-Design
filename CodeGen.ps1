# Description: This script will read a grammar file and generate a parser
# Note: The grammar (.grm) file must be a LL(1) grammar 
# Author: Mathieu Trudeau

$languageDict = @{}

function Is-Terminal {
    param(
        [string]$symbol
    )

    if ($symbol[0] -eq "'" -and $symbol[-1] -eq "'") {
        return $true
    }
    return $false
}

function Is-NonTerminal {
    param(
        [string]$symbol
    )

    if ($symbol[0] -eq "<" -and $symbol[-1] -eq ">") {
        return $true
    }
    return $false
}

function Extract-Name {
    param(
        [string]$symbol
    )

    $symbol = $symbol.Trim()

    if (Is-NonTerminal -symbol $symbol) {
        $prodName = $symbol.Replace("<", "").Replace(">", "")
        $prodName = $prodName.Substring(0, 1).ToUpper() + $prodName.Substring(1)
        $prodName = $prodName.Trim()

        if ($prodName -eq "START") {
            $prodName = "Start"
        }

        return $prodName
    }
    elseif (Is-Terminal -symbol $symbol) {
        $termName = $symbol.Replace("'", "")
        $termName = $termName.Trim()
        return $termName
    }
    else {
        $symbol = $symbol.Trim()
        return $symbol
    }
}

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
        "'void'" { return "Void" }
        "'while'" { return "While" }
        "'return'" { return "Return" }
        "'read'" { return "Read" }
        "'write'" { return "Write" }
        "'then'" { return "Then" }
        "'struct'" { return "Struct" }
        "'inherits'" { return "Inherits" }
        "'integer'" { return "Integer" }
        "'float'" { return "Float" }
        "'let'" { return "Let" }
        "'public'" { return "Public" }
        "'private'" { return "Private" }
        "$" { return "Eof" }
        Default {}
    }


    return $terminal
}

function Translate-Terminal {
    param(
        [string]$terminal
    )
    #Write-Host "Translating $terminal"

    $terminal = $terminal.Trim()

    $termName = "TokenType." + (Get-Enum-Value -terminal $terminal)

    return $termName
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

    #Write-Host "$left -> $right"
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
            $translatedTerm = Translate-Terminal -terminal $start

            $start = "$translatedTerm == LookAhead.Type"
        }
        else {
            $start = Extract-Name -symbol $start
            $start = "FIRST_$start.Contains(LookAhead.Type)"
        }


        

        $elements = $prod.Split(' ', [System.StringSplitOptions]::RemoveEmptyEntries)

        $recMatch = ""
        $recOutput = "$productionName ->"
        
        foreach ($symbol in $elements) {
            if (Is-Terminal -symbol $symbol) {
                if ($recMatch -ne "") {
                    $recMatch += " && "
                }
                $recMatch += "Match(" + (Translate-Terminal -terminal $symbol) + ")"
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
            if($recMatch)
                return OutputProductionRule("$recOutput");
            else
                return false;
"@
        }
        else {
            $methodCode += @"

        else if ($start)
        {
            OutputDerivation("$recOutput");
            if($recMatch)
                return OutputProductionRule("$recOutput");
            else
                return false;
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
            return OutputProductionRule("$productionName -> EPSILON");
        }
"@                
        }
        else {
            $methodCode += @"

        if (FOLLOW_$prodName.Contains(LookAhead.Type))
        {
            OutputDerivation("$productionName -> EPSILON");
            return OutputProductionRule("$productionName -> EPSILON");
        }
"@                
        }
    }

    $methodCode += @"

        else
            return OutputError($"Syntax error: Unexpected {LookAhead.Lexeme} at line {LookAhead.Location}.");
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

    private static readonly TokenType[] $prodName = new TokenType[] { $(($set | ForEach-Object { Translate-Terminal -terminal $_ }) -join ", ") };
"@
}

function Generate-First-Set-Rec {
    param(
        [string]$productionName,
        [System.Collections.Hashtable]$grammarDict,
        [System.Collections.ArrayList]$firstSet
    )

    $productions = $grammarDict[$productionName]

    foreach ($prod in $productions) {
        #Write-Host "Production: $prod"
        $elements = ($prod.Trim() -split " ")
        $first = $elements[0].Trim()
     
        #Write-Host "First element: $first"

        if (Is-Terminal -symbol $first) {
            # If the first element is a terminal, add it to the first set if it's not already there
            if (!$firstSet.Contains($first)) {
                #Write-Host "Adding $first to first set"
                $firstSet.Add($first) | Out-Null
            }
        }
        elseif (Is-NonTerminal -symbol $first) {
            # If the first element is a non-terminal, get the first set of that non-terminal
            $nt_firstSet = (Generate-First-Set-Rec -productionName $first -grammarDict $grammarDict -firstSet $firstSet)

            # Add the elements of the first set of the non-terminal to the first set of the current production
            foreach ($elem in $nt_firstSet) {
                if (!$firstSet.Contains($elem)) {
                    #Write-Host "Adding $elem to first set"
                    $firstSet.Add($elem) | Out-Null
                }
            }
        }
        else {
            if (!$firstSet.Contains("EPSILON")) {
                #Write-Host "Adding EPSILON to first set"
                $firstSet.Add("EPSILON") | Out-Null
            }
        }
    }

    return $firstSet
}

function Generate-First-Set {
    param(
        [string]$productionName,
        [System.Collections.Hashtable]$grammarDict
    )

    Write-Host "Generating first set for $productionName"

    $firstSet = New-Object System.Collections.ArrayList

    #Write-Host "Generating first set for $productionName"

    $firstSet = (Generate-First-Set-Rec -productionName $productionName -grammarDict $grammarDict -firstSet $firstSet)

    return $firstSet
}

function Generate-Follow-Set-Rec {
    param(
        [string]$productionName,
        [System.Collections.Hashtable]$grammarDict,
        [System.Collections.ArrayList]$followSet,
        [System.Collections.ArrayList]$visited
    )

    if ($visited.Contains($productionName)) {
        return $followSet
    }
    else {
        $visited.Add($productionName) | Out-Null
    }

    foreach ($key in $grammarDict.Keys) {
        $productions = $grammarDict[$key]

        if ($key -eq $productionName) {
            continue
        }

        foreach ($prod in $productions) {
            if ($prod.Contains($productionName)) {
                #Write-Host "Found $productionName in $key"
                #Write-Host "Production: $key -> $prod"
                
                $elements = $prod.Split(' ', [System.StringSplitOptions]::RemoveEmptyEntries)
                $count = $elements.Count - 1

                for ($i = 0; $i -le $count; $i++) {
                    if ($elements[$i] -eq $productionName) {
                        #Write-Host "Element: $($elements[$i])"
                        if ($i -lt $count) {
                            $next = $elements[$i + 1]

                            if (Is-Terminal -symbol $next) {
                                if (!$followSet.Contains($next)) {
                                    #Write-Host "Adding $next to follow set"
                                    $followSet.Add($next) | Out-Null
                                }
                            }
                            else {
                                $nt_firstSet = (Generate-First-Set -productionName $next -grammarDict $grammarDict)

                                foreach ($elem in $nt_firstSet) {
                                    if (!$followSet.Contains($elem) -and ($elem -ne "EPSILON")) {
                                        #Write-Host "Adding $elem to follow set"
                                        $followSet.Add($elem) | Out-Null
                                    }
                                }

                                if ($nt_firstSet.Contains("EPSILON")) {
                                    $nextFollow = (Generate-Follow-Set-Rec -productionName $next -grammarDict $grammarDict -followSet $followSet -visited $visited)

                                    foreach ($elem in $nextFollow) {
                                        if (!$followSet.Contains($elem)) {
                                            #Write-Host "Adding $elem to follow set"
                                            $followSet.Add($elem) | Out-Null
                                        }
                                    }
                                }
                            }
                            

                            #Write-Host "Next: $next"
                        }
                        else {
                            #Write-Host "Next: End of production"

                            $nextFollow = (Generate-Follow-Set-Rec -productionName $key -grammarDict $grammarDict -followSet $followSet -visited $visited)

                            foreach ($elem in $nextFollow) {
                                if (!$followSet.Contains($elem)) {
                                    #Write-Host "Adding $elem to follow set"
                                    $followSet.Add($elem) | Out-Null
                                }
                            }
                        }
                    }
                    
                }
            }
        }
    }

    return $followSet
}

function Generate-Follow-Set {
    param(
        [string]$productionName,
        [System.Collections.Hashtable]$grammarDict
    )

    $followSet = New-Object System.Collections.ArrayList
    $visited = New-Object System.Collections.ArrayList

    Write-Host "Generating follow set for $productionName"

    if ($productionName -eq "<START>") {
        $followSet.Add("$") | Out-Null
    }

    $followSet = (Generate-Follow-Set-Rec -productionName $productionName -grammarDict $grammarDict -followSet $followSet -visited $visited)

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
    
public class Parser : IParser
{
    private const bool DEBUG = true;
    private IScanner Scanner { get; }
    private Token LookAhead { get; set; }
    private string Source {get;set;} = "";
    private string SourceName {get;set;} = "";
    private const string OUT_SYNTAX_ERRORS_EXTENSION = ".outsyntaxerrors";
    private const string OUT_DERIVATION_EXTENSION = ".outderivation";
    private const string OUT_PRODUCTIONS_EXTENSION = ".outproductions";

    #region Constructor

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

        if (DEBUG)
            WriteLine("Parser initialized...");
    }

    #endregion Constructor

    #region Base Methods

    public bool Parse()
    {
        // Get the first token
        LookAhead = Scanner.NextToken();

        // Make sure the token is not a Comment
        while (LookAhead.Type == Blockcmt || LookAhead.Type == Inlinecmt)
            LookAhead = Scanner.NextToken();


        if (DEBUG)
            WriteLine("Parsing...");

        return Start() && Match(Eof);
    }

    private bool Match(TokenType tokenType)
    {
        if(tokenType == Eof)
            while(LookAhead.Type == Blockcmt || LookAhead.Type == Inlinecmt)
                LookAhead = Scanner.NextToken();

        // Check if the current token matches the expected token
        bool isMatch = LookAhead.Type == tokenType;

        if(!isMatch)
            WriteLine($"Syntax error: Unexpected {LookAhead.Lexeme} at line {LookAhead.Location}. Expected {tokenType}.");

        // Get the next token
        LookAhead = Scanner.NextToken();

        // Make sure the token is not a Comment
        while (LookAhead.Type == Blockcmt || LookAhead.Type == Inlinecmt)
            LookAhead = Scanner.NextToken();

        // Return the result
        return isMatch;
    }

    private bool SkipErrors(TokenType[] firstSet, TokenType[] followSet)
    {
        if (firstSet.Contains(LookAhead.Type) || (firstSet.Contains(Epsilon) && followSet.Contains(LookAhead.Type)))
            return true;
        else
        {
            WriteLine($"Syntax error: Unexpected {LookAhead.Lexeme} at line {LookAhead.Location}.");

            while (!firstSet.Contains(LookAhead.Type) && !followSet.Contains(LookAhead.Type))
            {
                LookAhead = Scanner.NextToken();
                if (firstSet.Contains(Epsilon) && followSet.Contains(LookAhead.Type))
                    return false;
            }
            return true;
        }
    }

    private void OutputDerivation(string productionRuleStr)
    {
        using StreamWriter sw = new(SourceName + OUT_DERIVATION_EXTENSION, true);
        sw.WriteLine(productionRuleStr);
    }

    private static bool OutputProductionRule(string productionRuleStr)
    {
        //WriteLine(productionRuleStr);
        return true;
    }

    private bool OutputError(string messageStr)
    {
        WriteLine(messageStr);
        using StreamWriter sw = new(SourceName + OUT_SYNTAX_ERRORS_EXTENSION, true);
        sw.WriteLine(messageStr);
        return false;
    }

    #endregion Base Methods

    #region First Sets

    $($grammarDict.Keys | ForEach-Object { Generate-Set-Code -productionName $_ -set (Generate-First-Set -productionName $_ -grammarDict $grammarDict) })

    #endregion First Sets

    #region Follow Sets
    $($grammarDict.Keys | ForEach-Object { Generate-Set-Code -productionName $_ -set (Generate-Follow-Set -productionName $_ -grammarDict $grammarDict) -isFollow $true })

    #endregion Follow Sets

    #region Productions

    $($grammarDict.Keys | ForEach-Object { Generate-Production-Method -productionName $_ -productions $grammarDict[$_] })

    #endregion Productions
}
"@


    return $code

    # foreach ($key in $grammarDict.Keys) {
    #     #Generate-Production-Method -productionName $key -productions $grammarDict[$key]
    #     $fset = (Generate-Follow-Set -productionName $key -grammarDict $grammarDict)
    #     Write-Host (Generate-Set-Code -productionName $key -set $fset -isFollow $true)

    # }
    

    
}



$grammar = Get-Content -Path "grammar.grm"

# Loop through each line in the grammar file and process the production
foreach ($line in $grammar) {
    if ($line -eq "") {
        continue
    }
    Process-Production -production $line
}


# Create the parser code
Generate-CSharp-Parser-Code -grammarDict $languageDict | Out-File -FilePath "SyntacticAnalyzer/Parser.cs" -Encoding utf8
