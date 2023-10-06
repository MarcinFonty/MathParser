internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
    }
}

public class Parser
{
    private Scope scope;
    private Dictionary<string, object> symbolTable = new Dictionary<string, object>();
    private Token token;
    private List<Token> tokens;
    private int tokenNr;

    private Token Itself()
    {
        return this.token;
    }

    private class Scope
    {
        public Dictionary<string, object> Definition { get; } = new Dictionary<string, object>();
        public Scope Parent { get; set; }

        public void Define(Token n)
        {
            if (Definition.TryGetValue(n.Value, out var t) && t is not null)
            {
                if (t is object)
                {
                    n.Error(t is ReservedToken ? "Already reserved." : "Already defined.");
                }

                Definition[n.Value] = n;
                n.Reserved = false;
                n.Nud = Itself;
                n.Led = null;
                n.Std = null;
                n.Lbp = 0;
                n.Scope = scope;
            }
        }

        public object Find(string n)
        {
            var e = this;
            object o;
            while (true)
            {
                o = e.Definition.TryGetValue(n, out var val) ? val : null;
                if (o is not null && o is not FuncToken)
                {
                    return o;
                }

                e = e.Parent;
                if (e is null)
                {
                    o = symbolTable.TryGetValue(n, out val) ? val : symbolTable["(name)"];
                    return o is not null && o is not FuncToken ? o : symbolTable["(name)"];
                }
            }
        }

        public void Pop()
        {
            scope = this.Parent;
        }

        public void Reserve(Token n)
        {
            if (n.Arity != "name" || n.Reserved)
            {
                return;
            }

            if (Definition.TryGetValue(n.Value, out var t) && t is not null)
            {
                if (t.Reserved)
                {
                    return;
                }

                if (t.Arity == "name")
                {
                    n.Error("Already defined.");
                }
            }

            Definition[n.Value] = n;
            n.Reserved = true;
        }
    }

    private Scope NewScope()
    {
        var s = scope;
        scope = new Scope { Parent = s };
        return scope;
    }

    private void Advance(string id = null)
    {
        string a, v;
        object o;
        if (id != null && token.Id != id)
        {
            token.Error("Expected '" + id + "'.");
        }

        if (tokenNr >= tokens.Count)
        {
            token = symbolTable["(end)"] as Token;
            return;
        }

        var t = tokens[tokenNr];
        tokenNr++;
        v = t.Value;
        a = t.Type;

        if (a == "name")
        {
            o = scope.Find(v);
        }
        else if (a == "punctuator")
        {
            o = symbolTable.TryGetValue(v, out var val) ? val : null;
            if (o is null)
            {
                t.Error("Unknown operator.");
            }
        }
        else if (a == "string" || a == "number")
        {
            o = symbolTable["(literal)"];
            a = "literal";
        }
        else
        {
            t.Error("Unexpected token.");
            o = null;
        }

        token = (Token)Activator.CreateInstance(o.GetType());
        token.LineNr = t.LineNr;
        token.ColumnNr = t.ColumnNr;
        token.Value = v;
        token.Arity = a;
    }

    private Token Expression(int rbp)
    {
        Token left;
        var t = token;
        Advance();
        left = t.Nud();
        while (rbp < token.Lbp)
        {
            t = token;
            Advance();
            left = t.Led(left);
        }
        return left;
    }

    private Token Statement()
    {
        var n = token;
        Token v;

        if (n.Std != null)
        {
            Advance();
            scope.Reserve(n);
            return n.Std();
        }
        v = Expression(0);
        if (!v.Assignment && v.Id != "(")
        {
            v.Error("Bad expression statement.");
        }
        Advance(";");
        return v;
    }

    private List<Token> Statements()
    {
        var a = new List<Token>();
        Token s;
        while (true)
        {
            if (token.Id == "}" || token.Id == "(end)")
            {
                break;
            }
            s = Statement();
            if (s != null)
            {
                a.Add(s);
            }
        }
        return a.Count == 0 ? null : a.Count == 1 ? new List<Token> { a[0] } : a;
    }

    private Token Block()
    {
        var t = token;
        Advance("{");
        return t.Std();
    }

    private class OriginalSymbol : Token
    {
        public OriginalSymbol()
        {
            Nud = () => Error("Undefined.");
            Led = left => Error("Missing operator.");
        }
    }

    private Token Symbol(string id, int bp = 0)
    {
        var s = symbolTable.TryGetValue(id, out var val) ? val : null;
        bp = bp > 0 ? bp : 0;
        if (s != null)
        {
            if (bp >= s.Lbp)
            {
                s.Lbp = bp;
            }
        }
        else
        {
            s = new OriginalSymbol { Id = id, Value = id, Lbp = bp };
            symbolTable[id] = s;
        }
        return s;
    }

    private Token Constant(string s, object v)
    {
        var x = Symbol(s);
        x.Nud = () =>
        {
            scope.Reserve(this);
            this.Value = symbolTable[this.Id].Value;
            this.Arity = "literal";
            return this;
        };
        x.Value = v;
        return x;
    }

    private Token Infix(string id, int bp, Func<Token, Token> led = null)
    {
        var s = Symbol(id, bp);
        s.Led = led ?? (left =>
        {
            this.First = left;
            this.Second = Expression(bp);
            this.Arity = "binary";
            return this;
        });
        return s;
    }

    private Token InfixR(string id, int bp, Func<Token, Token> led = null)
    {
        var s = Symbol(id, bp);
        s.Led = led ?? (left =>
        {
            this.First = left;
            this.Second = Expression(bp - 1);
            this.Arity = "binary";
            return this;
        });
        return s;
    }

    private Token Assignment(string id)
    {
        return InfixR(id, 10, left =>
        {
            if (left.Id != "." && left.Id != "[" && left.Arity != "name")
            {
                left.Error("Bad lvalue.");
            }
            this.First = left;
            this.Second = Expression(9);
            this.Assignment = true;
            this.Arity = "binary";
            return this;
        });
    }

    private Token Prefix(string id, Func<Token> nud = null)
    {
        var s = Symbol(id);
        s.Nud = nud ?? (() =>
        {
            scope.Reserve(this);
            this.First = Expression(70);
            this.Arity = "unary";
            return this;
        });
        return s;
    }

    private Token Stmt(string s, Func<Token> f)
    {
        var x = Symbol(s);
        x.Std = f;
        return x;
    }

    public Token Parse(List<Token> arrayOfTokens)
    {
        tokens = arrayOfTokens;
        tokenNr = 0;
        NewScope();
        Advance();
        var s = Statements();
        Advance("(end)");
        scope.Pop();
        return s;
    }
}

public class Token
{
    public string Type { get; set; }
    public string Value { get; set; }
    public int LineNr { get; set; }
    public int ColumnNr { get; set; }
    public string Id { get; set; }
    public string Arity { get; set; }
    public bool Reserved { get; set; }
    public bool Assignment { get; set; }

    public void Error(string message)
    {
        throw new Exception($"Error at Line {LineNr}, Column {ColumnNr}: {message}");
    }

    public virtual Token Nud()
    {
        Error("Undefined.");
        return this;
    }

    public virtual Token Led(Token left)
    {
        Error("Missing operator.");
        return this;
    }
}
