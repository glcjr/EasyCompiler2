using CSScriptLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using csscript;
using System.CodeDom.Compiler;

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
namespace EasyCompiler2
{

    public class EasyCompiler
    {
        
        protected IEvaluator evaluator = CSScript.Evaluator;
        protected Assembly _CompiledAssembly = null;

        public List<string> Assemblies { get; set; } = new List<string>();
        public List<string> SourceStrings { get; set; } = new List<string>();
        public List<string> SourceFiles { get; set; } = new List<string>();
        public string SourceString { get; set; } = "";
        public string InstanceToCreate { get; set; } = "";
        public string Namespace { get; set; } = "";
        public string MethodToInvoke { get; set; } = "";
        public object[] ParameterstoPass { get; set; } = null;
        public Assembly CompiledAssembly
        {
            get
            {
                return _CompiledAssembly;
            }
        }
        public int ErrorCount
        {
            get
            {
                return Errors.Count;
            }
        }
        public List<string> Errors { get; set; } = new List<string>();
        public bool Success
        {
            get
            {
                return ErrorCount == 0;
            }
        }
        public EasyCompiler()
        {
            CSScript.KeepCompilingHistory = true;
        }
        public void AddAssemblyLocations(params string[] assemblies)
        {
            foreach (var assembly in assemblies)
                AddAssemblyLocation(assembly);
        }
        //This method allows a single dll to be added to the assemblies list that is referenced in the source code that is being compiled.
        //The method makes sure the assemblies are unique in the list.
        public void AddAssemblyLocation(string assembly)
        {
            if (Assemblies.Contains(assembly))
                Assemblies.Remove(assembly);
            Assemblies.Add(assembly);
        }
        public void UseRosyln()
        {
            CSScript.EvaluatorConfig.Engine = EvaluatorEngine.Roslyn;
        }
        public void UseMono()
        {
            CSScript.EvaluatorConfig.Engine = EvaluatorEngine.Mono;
        }
        public void UseCodedom()
        {
            CSScript.EvaluatorConfig.Engine = EvaluatorEngine.CodeDom;
        }
        public void UseCS6()
        {
            CSScript.GlobalSettings.UseAlternativeCompiler = @"%CSSCRIPT_DIR%\Lib\CSSCodeProvider.v4.6.dll";
        }
        //A method to remove unneeded assmblys from the list
        public bool RemoveAssemblyLocation(string assembly)
        {
            if (Assemblies.Contains(assembly))
            {
                Assemblies.Remove(assembly);
                return true;
            }
            else
                return false;
        }
        //This method allows you to add a source string to the list of source strings that will be compiled.
        public void AddSourceString(string source)
        {
            if (SourceStrings.Contains(source))
                SourceStrings.Remove(source);
            SourceStrings.Add(source);
        }
        //This method allows you to add multiple source files to the source files list that will be compiled.
        public void AddSourceFiles(params string[] files)
        {
            foreach (var file in files)
                AddSourceFile(file);
        }
        //This methods adds a single source file to the source file list that will be compiled when the compile methods is called.
        //In order to successfully add a file, it must exist.
        public bool AddSourceFile(string file)
        {
            if (File.Exists(file))
            {
                if (SourceFiles.Contains(file))
                    SourceFiles.Remove(file);
                SourceFiles.Add(file);
                return true;
            }
            else
                return false;
        }
        // This method combines the source strings and source files lists together so the compile method can compile everything together.
        protected string[] GetAllSource()
        {
            List<string> Source = new List<string>();
            Source.Add(SourceString);
            foreach (string f in SourceFiles)
                Source.Add(File.ReadAllText(f));
            foreach (string s in SourceStrings)
                Source.Add(s);
            return Source.ToArray();
        }
        protected string BuildSource()
        {
            string[] sources = GetAllSource();
            List<string> usings = new List<string>();
            List<string> source = new List<string>();
            string temp;
            foreach (var s in sources)
            {
                temp = "";
                string[] sourcelines = s.Split(';');
                foreach (var l in sourcelines)
                    if (l.Trim().StartsWith("using"))
                    {
                        if (!(usings.Contains(l)))
                            usings.Add(l);
                        //temp = temp.Replace(l, "");
                    }
                    else
                    {
                        if (l != string.Empty)
                            temp += l + $";{Environment.NewLine}";
                        else
                            temp += $"{Environment.NewLine}";
                    }
                source.Add(temp);
            }
            return ReturnAsString(usings, true) + "\n" + ReturnAsString(source);
        }
        protected string ReturnAsString(List<string> usings, bool includesemi = false)
        {
            string temp = "";
            foreach (var u in usings)
            {
                if (u != string.Empty)
                {
                    temp += u.Replace("{", "{\n").Replace("}", "}\n").Trim();
                    if (includesemi)
                        temp += ";";
                    temp += $"{Environment.NewLine}";
                }
            }
            return temp;
        }
        protected void GetAssemblies()
        {
            foreach (var assem in Assemblies)
                if (File.Exists(assem))
                    evaluator = evaluator.ReferenceAssembly(assem);
                else
                {
                    evaluator.TryReferenceAssemblyByNamespace(assem, out bool resolved);
                    if (!resolved)
                        evaluator = evaluator.ReferenceAssemblyByName(assem);
                    else
                        evaluator = evaluator.ReferenceAssemblyByNamespace(assem);
                }
        }
        public bool Compile()
        {
            string SourcetoCompile = BuildSource();
            return Compile(SourcetoCompile);
        }
        public bool Compile(string SourcetoCompile)
        {

            evaluator = evaluator.ReferenceAssemblyOf(this);
            //evaluator.ReferenceAssemblyOf<string>();
            evaluator = evaluator.ReferenceAssembliesFromCode(SourcetoCompile);
            evaluator = evaluator.ReferenceDomainAssemblies();
            GetAssemblies();
            //  CompiledAssembly = CSScript.Evaluator.CompileCode(SourcetoCompile);

            try
            {
                _CompiledAssembly = evaluator.CompileCode(SourcetoCompile);

            }

            catch (CompilerException e)
            {
                Errors = (List<string>)e.Data["Errors"];

                Errors.Add(SourcetoCompile);

            }
            catch (Exception e)
            {
                Errors.Add(e.Message);                
            }
            //CSScript.CompilingHistory.Last().Value.Result.Errors.Count;

            // Results = CSScriptLibrary.CSScript.CompilingHistory.Last().Value.Result;// CSScript.LastCompilingResult.Result;
            return Success;
        }

        public object Invoke(string NameSpaceClassMethod, params object[] list)
        {
            try
            {
                _CompiledAssembly = CSScript.LoadCode(BuildSource());
                AsmHelper scriptAsm = new AsmHelper(_CompiledAssembly);

                return scriptAsm.Invoke(NameSpaceClassMethod, list);
            }
            catch (CompilerException e)
            {
                Errors = (List<string>)e.Data["Errors"];
                return Errors;
            }
            catch (Exception e)
            {
                Errors.Add(e.Message);
                return Errors;
            }
        }
        protected string StarOrValue(string tocheck)
        {
            if (tocheck.Equals(""))
            {
                return "*";
            }
            else
                return $"{tocheck}";
        }
        protected string FindNameSpace()
        {
            if (FindNameSpace(InstanceToCreate, out string NamespaceName))
            {
                return $"{NamespaceName}.";
            }
            else
                return "";
            //Assembly assem = _CompiledAssembly;
            //Type[] types = assem.GetTypes();
            //foreach (var t in types)
            //{
            //    if (t.Name.Equals(InstanceToCreate))
            //        return $"{t.Namespace}.";
            //}
            //return "";
        }
        protected string BuildInvoker()
        {
            string NS = StarOrValue(Namespace);
            if ((NS.Equals("*")) && (InstanceToCreate == string.Empty))
                NS = "";
            else if (NS.Equals("*"))
                NS = FindNameSpace();
            else
                NS = $"{NS}.";
            return $"{NS}{StarOrValue(InstanceToCreate)}.{StarOrValue(MethodToInvoke)}";
        }
        public bool FindClass(string methodname, out string ClassName)
        {
            ClassName = "";
            Assembly assem = _CompiledAssembly;
            Type[] ty = assem.GetTypes();
            Type thisone = ty[0];
            foreach (var tp in ty)
            {
                Console.WriteLine(tp.Name);
                MethodInfo[] methods = tp.GetMethods();
                foreach (var m in methods)
                {
                    Console.WriteLine($"Method:{m.Name}");
                    if (m.Name.Equals(methodname))
                    {
                        ClassName = tp.Name;
                        return true;
                    }
                }
            }
            return false;
        }
        public bool FindAllClasses(string methodname, out List<string> ClassNames)
        {
            ClassNames = new List<string>();
            Assembly assem = _CompiledAssembly;
            Type[] ty = assem.GetTypes();
            Type thisone = ty[0];
            foreach (var tp in ty)
            {
                Console.WriteLine(tp.Name);
                MethodInfo[] methods = tp.GetMethods();
                foreach (var m in methods)
                {
                    Console.WriteLine($"Method:{m.Name}");
                    if (m.Name.Equals(methodname))
                    {
                        ClassNames.Add(tp.Name);                       
                    }
                }
            }
            return ClassNames.Count > 0;
        }
        public bool FindNameSpace(string classname, out string NamespaceName)
        {
            Assembly assem = _CompiledAssembly;
            NamespaceName = "";
            Type[] types = assem.GetTypes();
            foreach (var t in types)
            {
                if (t.Name.Equals(classname))
                {
                    NamespaceName = t.Namespace;
                    return true;
                }
            }
            return false;
        }
        public bool FindAllNameSpaces(string classname, out List<string> NamespaceNames)
        {
            NamespaceNames = new List<string>();
            Assembly assem = _CompiledAssembly;            
            Type[] types = assem.GetTypes();
            foreach (var t in types)
            {
                if (t.Name.Equals(classname))
                {
                    NamespaceNames.Add(t.Namespace);                    
                }
            }
            return NamespaceNames.Count > 0;
        }
        public object Invoke(params object[] list)
        {
            try
            {
                _CompiledAssembly = CSScript.LoadCode(BuildSource());
                AsmHelper helper = new AsmHelper(CSScript.LoadCode(BuildSource(), null, false));
                return helper.Invoke(BuildInvoker(), list);                
            }
            catch (CompilerException e)
            {
                Errors = (List<string>)e.Data["Errors"];
                return Errors;
            }
            catch (Exception e)
            {
                Errors.Add(e.Message);
                return Errors;
            }
        }
        public object Invoke(bool usebuiltinparams)
        {
            try
            {
                _CompiledAssembly = CSScript.LoadCode(BuildSource());
                AsmHelper helper = new AsmHelper(CSScript.LoadCode(BuildSource(), null, false));
                if ((usebuiltinparams)&&(ParameterstoPass != null))
                    return helper.Invoke(BuildInvoker(), ParameterstoPass);
                else
                    return helper.Invoke(BuildInvoker());
            }
            catch (CompilerException e)
            {
                Errors = (List<string>)e.Data["Errors"];
                return Errors;
            }
            catch (Exception e)
            {
                Errors.Add(e.Message);
                return Errors;
            }
        }
        public object LaunchCompiledAssembly()
        {
            try
            {
                Assembly Compiled = _CompiledAssembly;
                int index = 0; int foundindex = 0;
                Type[] Types = Compiled.GetTypes();
                bool found = false;
                if (InstanceToCreate != string.Empty)
                {
                    foreach (var t in Types)
                    {
                        if (t.Name.Equals(InstanceToCreate))
                        {
                            foundindex = index;
                            found = true;
                        }
                        index++;
                    }
                    if (!(found))
                    {
                        InstanceToCreate = string.Empty;
                        return LaunchCompiledAssembly();
                    }
                }
                else
                {
                    foreach (var t in Types)
                    {
                        foreach (var m in t.GetMethods())
                            if (m.Name.Equals(MethodToInvoke))
                            {
                                foundindex = index;
                                found = true;
                            }
                        index++;
                    }
                }
                if (found)
                {
                    Type type = Compiled.GetTypes()[foundindex];
                    object obj = Activator.CreateInstance(type);
                    return type.InvokeMember(MethodToInvoke, BindingFlags.Default | BindingFlags.InvokeMethod, null, obj, ParameterstoPass);
                }
                else
                    throw new Exception("Unable to find Method in Assembly.");
            }
            catch (CompilerException e)
            {
                Errors = (List<string>)e.Data["Errors"];
                return Errors;
            }
            catch (Exception e)
            {
                Errors.Add(e.Message);
                return Errors;
            }
        }
    }
}
