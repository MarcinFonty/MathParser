internal class Program
{
    private static void Main(string[] args)
    {
        //string input = Console.ReadLine();
        Token[] tokens = Tokenize(Console.ReadLine());
        Parser parser = new Parser(tokens);

        foreach (var token in tokens) //uses this to ckeck if all the tokens are set correctly.
        {
            Console.WriteLine($"Type: {token.type}, Value: {token.value}");
        }

        Console.WriteLine("parsing succeeded");
    }

    static Token[] Tokenize(string input) //TODO: update the tokenizer to also set the Id for the tocken
    {
        Token[] tokens = new Token[input.Length]; // Create an array with the same length as the input string
        int tokenIndex = 0;

        string currentToken = "";

        foreach (char c in input)
        {
            if (char.IsDigit(c))
            {
                currentToken += c;
            }
            else if (IsOperator(c))
            {
                // Add the current number token (if any)
                if (!string.IsNullOrEmpty(currentToken))
                {
                    tokens[tokenIndex++] = new Token { type = "number", value = int.Parse(currentToken) };
                    currentToken = "";
                }

                tokens[tokenIndex++] = new Token { type = "operator", value = c.ToString() };
            }
            else if (!char.IsWhiteSpace(c))
            {
                throw new Exception($"Invalid character: {c}");
            }
        }

        // Add the last number token (if any)
        if (!string.IsNullOrEmpty(currentToken))
        {
            tokens[tokenIndex++] = new Token { type = "number", value = int.Parse(currentToken) };
        }

        // Resize the array to the actual number of tokens
        Array.Resize(ref tokens, tokenIndex);

        return tokens;
    }

    static bool IsOperator(char c) //TODO: probably could change the brackets to it's own type instead of operator or literal/number
    {
        return "+-*/()".Contains(c);
    }
}

internal class Parser
{
    public Dictionary<string, Symbol> symbolTable = new Dictionary<string, Symbol>(); //Keeps track of all registered symbols and how they should be handled
    public Token[] tokens; //List of all the inputs
    public Token currentToken; //Current token
    public int token_nr = 0; //Keeps track of token index
    public Parser(Token[] input)
    {
        tokens = input;
        currentToken = input[0];

        //Define closure //Basicly put it into the symbol table so it can be looked up by other functions to know when to end when they come across it.
        AddOrUpdateSymbol(")");

        //Define operators
        Infix("+", 50); //TODO: only an example, don't use these values untill you're sure of them
    }

    /// <summary>
    /// Creates new symbols and allows to keep track of which ones you have.
    /// </summary>
    /// <param name="id"> id is the symbol itself as a identefier </param>
    /// <param name="bp"> bp is binding power, basicaly it decides how the binary three would be ordered </param>
    /// <returns></returns>
    public Symbol AddOrUpdateSymbol(string id, int bp = 0)
    {
        if (symbolTable.ContainsKey(id))
        {
            if (bp >= symbolTable[id].LeftBindingPower) //updates binding power if the new assigned bp is higher than the existing one
            {
                symbolTable[id].LeftBindingPower = bp;
            }
        }
        else //create symbol if it doesn't excist in the symbol table
        {
            symbolTable[id] = new Symbol(bp);
        }

        return symbolTable[id];
    }

    public Symbol Infix(string id, int bp, Func<TreeNode, TreeNode> led = null)
    {
        var s = AddOrUpdateSymbol(id, bp);
        if (led == null)
        {
            s.Led = (left) => //TODO: Make an correct implementation for this
            {
                s.LeftOperand = left;
                s.RightOperand = Expression(bp);
                s.Arity = "binary";
                return s;
            };
        }
        else
        {
            s.Led = led;
        }

        return s;
    }

    public TreeNode Expression(int RightBindingPower)
    {
        var left = new TreeNode(); 
        var t = currentToken;
        Advance();
        left = t.Nud();
        while (RightBindingPower < currentToken.LeftBindingPower)
        {
            t = currentToken;
            Advance();
            left = t.Led(left);
        }
        return left;
    }

    /// <summary>
    /// Gets you the next token to work with from the input token array
    /// </summary>
    /// <param name="id"></param>
    /// <returns> workable token</returns>
    /// <exception cref="Exception"></exception>
    public Token Advance(string id = null) //transforms a simple token of the inputed tokens into one I can work with, aka takes from tokens and token_nr to make the currentToken. Important to understand tokens[token_nr] != currentToken;
    {
        string a;
        Symbol o;
        object v;

        if (id != null && currentToken.Id != id) //TODO: I don't understand this one, why does token contain definition of id as it already posseses it's value? Unsure about this but just copied the example code.
        {
            throw new Exception("Expected '" + id + "'.");
        }
        if(token_nr >= tokens.Length)
        {
            currentToken = new Token(); //unsure if I should use a new token or update the last token for the end. I'm guessing a new one is more suitable.
            currentToken.AddSymbolValues(symbolTable["(end)"]);
        }

        var t = tokens[token_nr];
        token_nr++;
        v = t.value;
        a = t.type;

        if(a == "operator")
        {
            if(!symbolTable.TryGetValue(v.ToString(), out o)) //sets the value of o, if key for o doesn't exist it goes inside the if statment and throws an exeption. This allows for a more gracefull error handling (which I don't do as I just throw it)
            {
                throw new Exception("unknow operator");
            }
        } else if(a == "number")
        {
            a = "literal";
            o = symbolTable["(literal)"];
        } else
        {
            throw new Exception("unexpected token");
        }

        currentToken = new Token();
        currentToken.AddSymbolValues(o);
        currentToken.value = v;
        currentToken.type = a;

        return currentToken;
    }
}

internal class Token : Symbol
{
    public string Id { get; set; }
    public string type { get; set; }
    public object value { get; set; }

    public void AddSymbolValues(Symbol symbol)
    {
        this.LeftBindingPower = symbol.LeftBindingPower;
        this.Led = symbol.Led;
        this.Nud = symbol.Nud;
    }
}

internal class Symbol : BaseSymbol
{
    public int LeftBindingPower { get; set; }

    // Properties to store left and right operands
    public TreeNode LeftOperand { get; set; }
    public TreeNode RightOperand { get; set; }

    // Property to specify the arity of the symbol (e.g., "binary")
    public string Arity { get; set; }
    public Symbol(int bp = 0)
    {
        LeftBindingPower = bp;
    }
}

internal class BaseSymbol //TODO: Posibly consider making these function abstracts
{
    public Func<TreeNode> Nud { get; set; } = () =>
    {
        throw new Exception("Undefiened");
    };

    public Func<TreeNode, Symbol> Led { get; set; } = (left) =>
    {
        throw new Exception("Missing operator");
    };
}

internal class TreeNode
{
    public char Operator { get; set; }
    public double Value { get; set; }
    public TreeNode Left { get; set; }
    public TreeNode Right { get; set; }
}