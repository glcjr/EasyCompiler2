using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EasyCompiler2;
/*********************************************************************************************************************************
Copyright and Licensing Message

This code is copyright 2018 Gary Cole Jr. 

This code is licensed by Gary Cole to others under the GPLv.3 https://opensource.org/licenses/GPL-3.0 
If you find the code useful or just feel generous a donation is appreciated.

Donate with this link: paypal.me/GColeJr
Please choose Friends and Family

Alternative Licensing Options

If you prefer to license under the LGPL for a project, https://opensource.org/licenses/LGPL-3.0
Single Developers working on their own project can do so with a donation of $20 or more. 
Small and medium companies can do so with a donation of $50 or more. 
Corporations can do so with a donation of $1000 or more.


If you prefer to license under the MS-RL for a project, https://opensource.org/licenses/MS-RL
Single Developers working on their own project can do so with a donation of $40 or more. 
Small and medium companies can do so with a donation of $100 or more.
Corporations can do so with a donation of $2000 or more.


if you prefer to license under the MS-PL for a project, https://opensource.org/licenses/MS-PL
Single Developers working on their own project can do so with a donation of $1000 or more. 
Small and medium companies can do so with a donation of $2000 or more.
Corporations can do so with a donation of $10000 or more.


If you use the code in more than one project, a separate license is required for each project.


Any modifications to this code must retain this message. 
*************************************************************************************************************************************/
namespace testcompiler
{
    class Program
    {
        static void Main(string[] args)
        {
            string sourcestring = "using System;\n using System.Windows.Forms;\n namespace HelloWorld { class Hello { static public int Add(int a, int b) {return a + b;} static public void Main(params string[] Args) { string message = \"\"; if (Args.Length > 0) message = Args[0];Console.WriteLine(\"Hello World \" + message);    if (message.Contains(\"off\")) MessageBox.Show(\"Good Bye\"); Console.WriteLine(\"Press a key\"); Console.ReadKey();      }        }    }";
            EasyCompiler ec = new EasyCompiler();
           ec.AddSourceString(sourcestring);
           // ec.UseCodedom();
            ec.MethodToInvoke = "Main";
            ec.AddAssemblyLocation("System.Windows.Forms");
            Console.WriteLine("Attempting to Compile the source");
            if (ec.Compile())
            {
                ec.ParameterstoPass = new string[] { "Let's get started" };
                Console.WriteLine("Launching assmebly with first input: ");
                ec.LaunchCompiledAssembly();
                ec.MethodToInvoke = "Add";
                ec.ParameterstoPass = new object[] { 1, 2 };
                Console.WriteLine("Accessing Add Method in assembly");
                object result = ec.LaunchCompiledAssembly();
                Type t = result.GetType();
                if (t.Equals(typeof(int)))
                {
                    Console.WriteLine(result);
                }
                Console.Write("Accessing number of any compile errors so far: ");
                Console.WriteLine(ec.ErrorCount);
                Console.WriteLine("Listing and Searching Methods of Assembly:");
                // Search through all methods in Assembly
                Assembly assem = ec.CompiledAssembly;
                Type[] ty = assem.GetTypes();
                Type thisone = ty[0];
                foreach (var tp in ty)
                {
                    Console.WriteLine(tp.Name);
                    MethodInfo[] methods = tp.GetMethods();
                    foreach (var m in methods)
                    {
                        Console.WriteLine($"Method:{m.Name}");
                        if (m.Name.Equals(ec.MethodToInvoke))
                            thisone = tp; // finds the type the method belongs to
                    }
                }
                Console.Write("The class with the method targeted inside:");
                Console.WriteLine(thisone.Name);
                if (ec.ErrorCount == 0)
                {
                    ec.InstanceToCreate = thisone.Name;
                    ec.MethodToInvoke = "Main";
                    ec.ParameterstoPass = new string[] { "And we're off." };
                    ec.LaunchCompiledAssembly();
                }
            }
            else
            {
                foreach (var e in ec.Errors)
                    Console.WriteLine(e.ToString());
            }
            ec.MethodToInvoke = "Add";
            Console.WriteLine("Using Add Method with Namespace.Class.Nethod supplied");
            object answer = ec.Invoke("HelloWorld.Hello.Add", new object[] { 5, 2 });
            Console.WriteLine(answer);
            ec.InstanceToCreate = "";
            ec.Namespace = "";
            Console.WriteLine("Using Add Method with just Method name supplied");
            answer = ec.Invoke(new object[] { 9, 45 });
            Console.WriteLine(answer);
             ec.InstanceToCreate = "Hello";
            Console.WriteLine("Using Add Method with just class and method names supplied ");
            answer = ec.Invoke(new object[] { 5, 25 });
            Console.WriteLine(answer);
            // check with bad input
            answer = ec.Invoke(new object[] { 2 });
            Console.WriteLine("Should be error with not enough paramters:");
            if (ec.Success)
                Console.WriteLine(answer);
            else
                foreach (var e in (List<string>)answer)
                    Console.WriteLine(e);
            Console.WriteLine("Using Add with everything supplied separately");
            ec.Namespace = "HelloWorld";
            answer = ec.Invoke(new object[] { 15, 2 });
            Console.WriteLine(answer);
            ec.Namespace = "";
            ec.InstanceToCreate = "";
            Console.WriteLine("Find with methods inside easycompile:");
            if (ec.FindClass(ec.MethodToInvoke, out string instance))
            {
                ec.InstanceToCreate = instance;
                answer = ec.Invoke(new object[] { 7, 13 });
                Console.WriteLine(answer);
            }
            else
                Console.WriteLine("Failed to find with inside ec");

        }
    }
}
