//{ Addition: 2 # 2 } = 4 Works
//{ Multiplication: { Subtraction: 8 # 2 } # 3 } Works
//{ Addition: { Addition: { Addition: { Addition: 4 # 4 } # 4 } # 4 } # 4 } works
//{ Subtraction: 122 # { Division: 8 # 4 } } works
//{ Addition: { Addition: 4 # { Addition: 4 # 4 } } # 4 } works

// { Division: { Subtraction: 12 # 3 } # { Multiplication: 1 # 3 } } working on it

//{ Multuplication: 4 # 4 # 4 } = 64 //TODO: doesn't work with three values
//{ Addition: 4 # {Subtraction: 4 # 12} # 2 } = -2 //TODO: doesn't work as the multiplication of previouse problems

using System;
using System.Data;

internal class Program
{
    private static void Main(string[] args)
    {
        while (true)
        {
            Parser parser = new Parser();
            string input = Console.ReadLine();
            var data = input.Split(' ');
            parser.Parse(data);
            Console.WriteLine("result is " + parser.Parse(data).ToString());
        }
    }
}

internal class Parser
{
    private int i = 0; //counts amount nested
    public int Parse(string[] data)
    {
        int result = 0;
        //for (int i = 0; i < data.Length; i++)
        //{
            if (data[0] == "{")
            {
                try
                {
                    var res = CheckValue(data[0 + 2], (0 + 2), data[0 + 4], (0 + 4), ref data);
                    if (res != null)
                    {
                        if (data[0 + 2] == "{")
                        {
                            data = replacementValueA(data, res.ToString());
                        }
                        else if (data[0 + 4] == "{")
                        {
                        data = replacementValueB(data, res.ToString());
                        }
                    i += 5;
                    }
                }   
                catch (Exception)
                {
                    //continue;
                }

                switch (data[0 + 1])
                {
                    case "Addition:":
                        result = Addition(int.Parse(data[0 + 2]), int.Parse(data[0 + 4]));
                        break;
                    case "Subtraction:":
                        result = Subtraction(int.Parse(data[0 + 2]), int.Parse(data[0 + 4]));
                        break;
                    case "Multiplication:":
                        result = Multiplication(int.Parse(data[0 + 2]), int.Parse(data[0 + 4]));
                        break;
                    case "Division:":
                        result = Devision(int.Parse(data[0 + 2]), int.Parse(data[0 + 4]));
                        break;
                    default:
                        break;
                }
            }
            else if (data[0] == "}")
            {
                //break;
            }
        //}
        return result;
    }
    private int? CheckValue(string a, int indexA, string b, int indexB, ref string[] data)
    {
        if (a == "{") 
        {
            var subdata = ExtractSubdata(indexA, data);
            var result = Parse(subdata);
            return result;
        }
        else if (b == "{")
        {
            var subdata = ExtractSubdata(indexB, data);
            var result = Parse(subdata);
            return result;
        }
        return null;
    }

    private string[] replacementValueA(string[] data, string replacementValue)
    {
        // Remove all values of the subdata array starting from the specified index
        for (int i = 2; i <= 7 + this.i; i++)
        {
            data[i] = replacementValue;
        }

        for (int i = 8 + this.i; i < data.Length; i++)
        {
            data[i - (5 + this.i)] = data[i];
        }

        Array.Resize(ref data, data.Length - 5);

        return data;
    }

    private string[] replacementValueB(string[] data, string replacementValue)
    {
        // Remove all values of the subdata array starting from the specified index
        for (int i = 4; i <= 9 + this.i; i++)
        {
            data[i] = replacementValue;
        }

        for (int i = 10 + this.i; i < data.Length; i++)
        {
            data[i - 5] = data[i];
        }

        Array.Resize(ref data, data.Length - 5 + this.i);

        return data;
    }

    private string[] ExtractSubdata(int index, string[] data)
    {
        List<string> subdataList = new List<string>();
        int openBracketCount = 0;

        // Start the loop at the specified index
        for (int i = index; i < data.Length; i++)
        {
            // Add the current element to the subdata list
            subdataList.Add(data[i]);

            // Check if the current element is "{" or "}"
            if (data[i] == "{")
            {
                openBracketCount++;
            }
            else if (data[i] == "}")
            {
                openBracketCount--;

                // If we've encountered a closing "}" that matches the corresponding "{", stop adding to subdata
                if (openBracketCount == 0)
                {
                    break;
                }
            }
        }
        // Convert the List<string> to an array
        string[] subdata = subdataList.ToArray();

        return subdata;
    }
    private int Addition(int a, int b)
    {
        return a + b;
    }
    private int Subtraction(int a, int b)
    {
        return a - b;
    }
    private int Multiplication(int a, int b)
    {
        return a * b;
    }
    private int Devision(int a, int b)
    {
        return a / b;
    }
}