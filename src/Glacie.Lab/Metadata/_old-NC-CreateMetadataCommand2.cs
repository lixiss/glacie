using System;
using System.Collections.Generic;
using System.IO;

using Glacie.CommandLine.IO;
using Glacie.Data.Tpl;

namespace Glacie.Lab.Metadata
{
    // controllermonsterhidden.tpl -> class should be ControllerMonsterHidden, because it should not be overriden by include
    // npcwanderpoint.tpl -> class should be NpcWanderPoint -> similar to controllermonsterhidden.
    // ormenosdropzone.tpl -> class should be OrmenosDropZone -> similar to NpcWanderPoint.

    // dynamicbarrier.tpl -> variable 'invincible' overrides defaultValue after included template.
    //   So, overriding variable after included template it introduced looks like intended.
    // endlessmodecontroller.tpl -> include go after variables (eqnVariables)

    // gamepadbuttonsdescriptionbox.tpl -> violate include order

    public sealed class CreateMetadataCommand2 : Command
    {
        private ITemplateResolver _templateResolver;

        protected override void RunCore()
        {
            // TODO: Configuration to get game data, glacie.lab.config.

            // var templatesDir = @"G:\Glacie\glacie-test-data\tpl\tqae-2.9\templates";
            var templatesDir = @"G:\Glacie\glacie-test-data\tpl\gd-1.1.7.1\templates";
            _templateResolver = new FileSystemTemplateResolver(templatesDir);


            foreach (var f in Directory.EnumerateFiles(templatesDir, "*.tpl", SearchOption.AllDirectories))
            {
                //if (f.EndsWith("controllermonsterhidden.tpl", StringComparison.OrdinalIgnoreCase)) continue;
                //if (f.EndsWith("dynamicbarrier.tpl", StringComparison.OrdinalIgnoreCase)) continue;
                //if (f.EndsWith("npcwanderpoint.tpl", StringComparison.OrdinalIgnoreCase)) continue;
                //if (f.EndsWith("ormenosdropzone.tpl", StringComparison.OrdinalIgnoreCase)) continue;
                //if (f.EndsWith("gamepadbuttonsdescriptionbox.tpl", StringComparison.OrdinalIgnoreCase)) continue;

                if (f.EndsWith("copy of lootitemtable_dynweightdynaffix.tpl", StringComparison.OrdinalIgnoreCase)) continue;
                if (f.EndsWith("copy of lootitemtable_dynweighted_dynaffix.tpl", StringComparison.OrdinalIgnoreCase)) continue;

                // Try to build record definition from .tpl files...
                ParseTemplate(f); // "Database\\Templates\\monster.tpl");

            }

        }

        private void ParseTemplate(string templateName)
        {
            Console.Out.WriteLine("ParseTemplate: {0}", templateName);

            List<TemplateVariable> variableList = new List<TemplateVariable>();
            Dictionary<string, TemplateVariable> variableMap = new Dictionary<string, TemplateVariable>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, TemplateVariable> eqnVariables = new Dictionary<string, TemplateVariable>();

            ProcessTemplate(templateName, (v) =>
            {
                if (v.Type == "eqnVariable")
                {
                    if (v.Name != "Object Variable")
                    {
                        throw Error.InvalidOperation("Invalid eqnVariable (name must be Object Variable).");
                    }

                    if (eqnVariables.TryGetValue(v.DefaultValue, out var cv))
                    {
                        if (v.Type == cv.Type
                            && v.Class == cv.Class
                            && v.Description == cv.Description
                            && v.Value == cv.Value
                            && v.DefaultValue == cv.DefaultValue)
                        {
                            // non-conflicting adding, may override to recent
                        }
                        else
                        {
                            Console.Out.WriteLine("Conflict Is Not Resolved: {0}: {1} -> {2} (???)", v.Name, cv.DefaultValue, v.DefaultValue);
                            // variableMap[v.Name] = v;
                        }
                    }
                    else
                    {
                        eqnVariables.Add(v.DefaultValue, v);
                    }
                    return;
                }
                else
                {
                    Check.That(v.Type != "include");

                    variableList.Add(v);
                    if (variableMap.TryGetValue(v.Name, out var cv))
                    {
                        if (v.Type == cv.Type
                            && v.Class == cv.Class
                            && v.Description == cv.Description
                            && v.Value == cv.Value
                            && v.DefaultValue == cv.DefaultValue)
                        {
                            // non-conflicting adding, may override to recent
                            // variableMap[v.Name] = v;
                        }
                        else
                        {
                            Console.Out.WriteLine("Conflict Blocked: {0}: {1} -> {2}", v.Name, v.DefaultValue, cv.DefaultValue);
                            // variableMap[v.Name] = v;
                        }
                    }
                    else
                    {
                        variableMap.Add(v.Name, v);
                    }
                }


                //if (g.Type == "system")
                //{
                //    variableMap[v.Name] = v;
                //    // TODO: remove from list old variable
                //}
                //else
                //{
                //    variableMap.Add(v.Name, v);
                //}
                // Console.Out.WriteLine("{0} : {1}", v.Name, v.Type);
            });

            //Console.Out.WriteLine("# total variables: {0}", variableList.Count);
            //Console.Out.WriteLine("# unique variables: {0}", variableMap.Count);

            //foreach (var v in variableMap.Values)
            //{
            //    Console.Out.WriteLine("{0} = {1} : {2}", v.Name, v.Type, v.Class);
            //}

        }

        private void ProcessTemplate(string templateName, Action<TemplateVariable> addVariable)
        {
            // Console.Out.WriteLine("# Processing: {0}", templateName);
            var template = _templateResolver.ResolveTemplate(templateName);
            ProcessTemplateGroup(template.Root, addVariable);
        }

        private void ProcessTemplateGroup(TemplateGroup templateGroup, Action<TemplateVariable> addVariable)
        {
            foreach (var v in AllVariables(templateGroup))
            {
                if (v.Type == "include") continue;

                addVariable(v);
            }

            // process includes
            foreach (var v in AllVariables(templateGroup))
            {
                if (v.Type == "include")
                {
                    ProcessTemplate(v.DefaultValue, addVariable);
                }
            }



            return;

            var disallowInclude = false;
            // Console.Out.WriteLine("Group: {0} : {1}", templateGroup.Name, templateGroup.Type);
            foreach (var node in templateGroup.Children)
            {
                if (node is TemplateVariable v)
                {
                    if (v.Type == "include")
                    {
                        if (disallowInclude)
                        {
                            throw Error.InvalidOperation("Include directives must go first.");
                        }

                        // Console.Out.WriteLine("Including... {0}", v.DefaultValue);
                        ProcessTemplate(v.DefaultValue, addVariable);
                        // Console.Out.WriteLine("Included", v.DefaultValue);
                    }
                    else if (v.Type == "eqnVariable")
                    {
                        // disallowInclude = true;

                        if (v.Name != "Object Variable")
                        {
                            throw Error.InvalidOperation("Invalid eqnVariable (name must be Object Variable).");
                        }

                        // Skip them, but should add to RecordDefinition?
                        //Console.Out.WriteLine("eqnVariable: {0}", v.DefaultValue);
                    }
                    else
                    {
                        //disallowInclude = true;
                        //Console.Out.WriteLine("{0} : {1}", v.Name, v.Type);
                        addVariable(v);
                    }
                }
                else if (node is TemplateGroup g)
                {
                    //disallowInclude = true;
                    ProcessTemplateGroup(g, addVariable);
                }
                else throw Error.Unreachable();
            }
        }

        private IEnumerable<TemplateVariable> AllVariables(TemplateGroup templateGroup)
        {
            foreach (var node in templateGroup.Children)
            {
                if (node is TemplateVariable v)
                {
                    yield return v;
                }
                else if (node is TemplateGroup g)
                {
                    foreach (var x in AllVariables(g)) yield return x;
                }
            }
        }
    }
}
