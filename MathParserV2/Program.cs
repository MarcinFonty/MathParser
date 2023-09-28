using System;

using System.Data;

using System.Diagnostics;

using System.Reflection;

using System.Threading.Tasks.Dataflow;

 

internal class Program

{

    private static void Main(string[] args)
    {
        //string input = Console.ReadLine();
        Token[] tokens = Tokenize(Console.ReadLine());
        //Parser paser = new Parser(tokens);

        foreach (var token in tokens)
        {
            Console.WriteLine($"Id: {token.Id}, Type: {token.Type}, Value: {token.Value}");
        }

        Console.WriteLine("parsing succeeded");
    }



    static Token[] Tokenize(string input)
    {
        Token[] tokens = new Token[input.Length];
        int tokenIndex = 0;

        foreach (char c in input)
        {
            if (char.IsDigit(c))
            {
                tokens[tokenIndex++] = new Token(c, 0)
                {
                    Type = "number",
                    Value = int.Parse(c.ToString())
                };
            }

            else if (IsOperator(c))
            {
                tokens[tokenIndex++] = new Token(c)
                {
                    Type = "operator",
                    Value = c.ToString()
                };
            }
            else if (!char.IsWhiteSpace(c))
            {
                throw new Exception($"Invalid character: {c}");
            }
        }

        // Resize the array to the actual number of tokens
        Array.Resize(ref tokens, tokenIndex);

        return tokens;
    }

    static bool IsOperator(char c)
    {
        return "+-*/()".Contains(c);
    }
}



internal class Parser

{

    public Dictionary<char, Token> /*id, pb*/ symbolTable = new Dictionary<char, Token>(); //Keeps track of all registered symbols and how they should be handled

    public Token[] tokens; //List of all the inputs

    public Token currentToken; //Current token

    public int token_nr = 0; //Keeps track of token index

    public TreeNode root = new TreeNode();



    //Using 'E' as end of tokens



    public Parser(Token[] input)

    {

        tokens = input;

    }



    public Token AddOrUpdateSymbol(char id, int bp = 0)

    {

        if (symbolTable.ContainsKey(id))

        {

            if (bp >= symbolTable[id].BindingPower)

            {

                symbolTable[id].BindingPower = bp;

            }

        }

        else

        {

            symbolTable[id] = new Token(id, bp);

        }



        return symbolTable[id];

    }



    private Token Advance(char id = 'N') // char id = null apperantly doesn't work. So for now I take N as a null

    {

        if (id != 'N' && id == currentToken.Id)

        {

            throw new Exception("Expected '" + id + "'.");

        }

        if (token_nr >= tokens.Count())

        {

            currentToken = symbolTable['E']; //Using 'E' as end of tokens

            return null;

        }

        var t = tokens[token_nr];

        ++token_nr;

        return t;

        //var v = t.Id;

        //var a = t.Type;

        //Token o = null;

        //if (a == "operator")

        //{

        //    o = symbolTable[v];

        //    if (o != null)

        //    {

        //        throw new Exception("Unknown operator.");

        //    }

        //}

        //else if (a === "string" || a === "number")

        //{

        //    a = "literal";

        //    o = symbolTable["(literal)"];

        //}

        //else

        //{

        //    throw new Exception("Unexpected token.");

        //}

        //var token = o;

        //token.Id = v;

        //token.Type = a;

        //return token;

    }



    private TreeNode Expression(int rbp)

    {

        TreeNode left;

        Token t = currentToken;

        Advance();

        left = t.Nud();

        while (rbp < currentToken.BindingPower)

        {

            t = currentToken;

            Advance();

            left = t.Led(left);

        }

        return left;

    }



    private Token InfixLeft(char id, int bp, Func<TreeNode, TreeNode> led = null)

    {

        var s = AddOrUpdateSymbol(id, bp);



        if (led != null)

        {

            s.Led = led;

        }

        else

        {

            s.Led = ((left) =>

            {

                TreeNode tree = new();

                tree.Left = left;

                tree.Right = Expression(bp);

                //s.Arity = "binary";

                return tree;

            });

        }

        return s;

    }



    private void InfixRight(char id, int bp, Func<Token, Token> led = null)

    {





    }

}



internal class Token : BaseToken

{

    public char Id { get; set; }

    public string Type { get; set; }

    public object Value { get; set; }

    public int BindingPower { get; set; } = 0;

    public Token(char id, int bp)

    {

        Id = id;

        BindingPower = bp;

    }



    public Token(char id)

    {

        Id = id;

    }

}



internal class BaseToken

{

    public Func<TreeNode> Nud { get; set; } = () =>

    {

        throw new Exception("Undefiened");

    };



    public Func<TreeNode, TreeNode> Led { get; set; } = (left) =>

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