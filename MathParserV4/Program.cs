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
                    tokens[tokenIndex++] = new Token { type = Type.literal, value = int.Parse(currentToken) };
                    currentToken = "";
                }

                tokens[tokenIndex++] = new Token { type = Type.literal, value = c.ToString() };
            }
            else if (!char.IsWhiteSpace(c))
            {
                throw new Exception($"Invalid character: {c}");
            }
        }

        // Add the last number token (if any)
        if (!string.IsNullOrEmpty(currentToken))
        {
            tokens[tokenIndex++] = new Token { type = Type.literal, value = int.Parse(currentToken) };
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
    public Dictionary<string, Symbol> symbolTable = new Dictionary<string, Symbol>();
    public Token[] tokens; 
    public Token currentToken; 
    public int token_nr = 0; 
    public Parser(Token[] input)
    {
        tokens = input;
        currentToken = input[0];

        AddOrUpdateSymbol(")");

        Infix("+", 50);
        Infix("-", 50);
        Infix("*", 60);
        Infix("/", 60);
        InfixR("^", 70);
        //InfixR("√", 70);

        AddOrUpdateSymbol("(literal)").Nud = () =>
        {
            return AddOrUpdateSymbol("(literal)");
        };
    }

    public Symbol AddOrUpdateSymbol(string id, int bp = 0)
    {
        if (symbolTable.ContainsKey(id))
        {
            if (bp >= symbolTable[id].LeftBindingPower)
            {
                symbolTable[id].LeftBindingPower = bp;
            }
        }
        else 
        {
            symbolTable[id] = new Symbol(bp);
        }

        return symbolTable[id];
    }

    public Symbol Infix(string id, int bp, Func<Symbol, Symbol> led = null)
    {
        var s = AddOrUpdateSymbol(id, bp);
        if (led == null)
        {
            s.Led = (left) =>
            {
                s.First = left;
                s.Second = Expression(bp);
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

    public Symbol InfixR(string id, int bp, Func<Symbol, Symbol> led = null)
    {
        var s = AddOrUpdateSymbol(id, bp);
        if (led == null)
        {
            s.Led = (left) =>
            {
                s.First = left;
                s.Second = Expression(bp - 1);
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

    public Symbol Expression(int RightBindingPower)
    {
        var left = new Symbol();
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

    public Token Advance(string id = null) //transforms a simple token of the inputed tokens into one I can work with, aka takes from tokens and token_nr to make the currentToken. Important to understand tokens[token_nr] != currentToken;
    {
        Type a;
        Symbol o;
        object v;

        if (id != null && currentToken.Id != id) //TODO: I don't understand this one, why does token contain definition of id as it already posseses it's value? Unsure about this but just copied the example code.
        {
            throw new Exception("Expected '" + id + "'.");
        }
        if (token_nr >= tokens.Length)
        {
            currentToken = new Token(); //unsure if I should use a new token or update the last token for the end. I'm guessing a new one is more suitable.
            currentToken.AddSymbolValues(symbolTable["(end)"]);
        }

        var t = tokens[token_nr];
        token_nr++;
        v = t.value;
        a = t.type;

        if (a == Type.Operator)
        {
            if (!symbolTable.TryGetValue(v.ToString(), out o)) //sets the value of o, if key for o doesn't exist it goes inside the if statment and throws an exeption. This allows for a more gracefull error handling (which I don't do as I just throw it)
            {
                throw new Exception("unknow operator");
            }
        }
        else if (a == Type.literal)
        {
            o = symbolTable["(literal)"];
        }
        else
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
    public Type type { get; set; }
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
    public Symbol First { get; set; }
    public Symbol Second { get; set; }

    // Property to specify the arity of the symbol (e.g., "binary")
    public string Arity { get; set; }
    public Symbol(int bp = 0)
    {
        LeftBindingPower = bp;
    }
}

internal class BaseSymbol //TODO: Posibly consider making these function abstracts
{
    public Func<Symbol> Nud { get; set; } = () =>
    {
        throw new Exception("Undefiened");
    };

    public Func<Symbol, Symbol> Led { get; set; } = (left) =>
    {
        throw new Exception("Missing operator");
    };
}

internal enum Type
{
    Operator,
    literal,
    variable
}