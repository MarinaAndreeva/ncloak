using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace TiviT.NCloak.CloakTasks
{
    public class CombineLocals : ICloakTask
    {
        public string Name
        {
            get { return "Combining locals"; }
        }

        public void RunTask(ICloakContext context)
        {
            //Go through each assembly 
            //for each assembly inject a decryption routine - we'll let the obfuscator hide it properly
            //Loop through each assembly and obfuscate it
            foreach (AssemblyDefinition definition in context.GetAssemblyDefinitions().Values)
            {
                CombineLocalsInAssembly(definition);
            }
        }

        private void CombineLocalsInAssembly(AssemblyDefinition definition)
        {
            foreach (var moduleDefinition in definition.Modules)
            {
                TypeReference int32_type = moduleDefinition.Import(typeof (Int32));
                TypeReference int64_type = moduleDefinition.Import(typeof (Int64));

                foreach (var typeDefinition in moduleDefinition.Types)
                {
                    foreach (var methodDefinition in typeDefinition.Methods)
                    {
                        if (methodDefinition.HasBody == false)
                        {
                            continue;
                        }

                        methodDefinition.Body.SimplifyMacros();
                        var il = methodDefinition.Body.GetILProcessor();

                        List<VariableDefinition> intVarsDefinitions = new List<VariableDefinition>();
                        foreach (var variableDefinition in methodDefinition.Body.Variables)
                        {
                            if (variableDefinition.VariableType.FullName == int32_type.FullName)
                            {
                                bool ignoreInstruction = null != methodDefinition.Body.Instructions.FirstOrDefault(
                                    instruction =>
                                        instruction.OpCode == OpCodes.Ldloca &&
                                        instruction.Operand == variableDefinition);

                                if (ignoreInstruction == false)
                                {
                                    intVarsDefinitions.Add(variableDefinition);
                                }
                            }
                        }

                        while (intVarsDefinitions.Count >= 2)
                        {
                            VariableDefinition firstVar = intVarsDefinitions.First();
                            intVarsDefinitions.Remove(firstVar);
                            VariableDefinition secondVar = intVarsDefinitions.First();
                            intVarsDefinitions.Remove(secondVar);

                            methodDefinition.Body.Variables.Remove(firstVar);
                            methodDefinition.Body.Variables.Remove(secondVar);

                            VariableDefinition zVar = new VariableDefinition(int64_type);
                            methodDefinition.Body.Variables.Add(zVar);
                            methodDefinition.Body.Instructions.Insert(0, il.Create(OpCodes.Ldc_I8, (long) 0));
                            methodDefinition.Body.Instructions.Insert(1, il.Create(OpCodes.Stloc, zVar.Index));

                            Dictionary<Instruction, List<Instruction>> listStLdloc =
                                new Dictionary<Instruction, List<Instruction>>();
                            foreach (var instruction in methodDefinition.Body.Instructions)
                            {
                                if ((instruction.OpCode == OpCodes.Ldloc) && (instruction.Operand == firstVar))
                                {
                                    listStLdloc.Add(instruction, GenerateLdlocXReplacement(zVar, il));
                                    //GenerateLdlocXReplacement(zVar.Index, methodDefinition.Body.GetILProcessor());
                                }
                                if ((instruction.OpCode == OpCodes.Ldloc) && (instruction.Operand == secondVar))
                                {
                                    listStLdloc.Add(instruction, GenerateLdlocYReplacement(zVar, il));
                                    //GenerateLdlocYReplacement(zVar.Index, methodDefinition.Body.GetILProcessor());
                                }
                                if ((instruction.OpCode == OpCodes.Stloc) && (instruction.Operand == firstVar))
                                {
                                    listStLdloc.Add(instruction, GenerateStlocXReplacement(zVar, il));
                                    //GenerateStlocXReplacement(zVar.Index, methodDefinition.Body.GetILProcessor());
                                }
                                if ((instruction.OpCode == OpCodes.Stloc) && (instruction.Operand == secondVar))
                                {
                                    listStLdloc.Add(instruction, GenerateStlocYReplacement(zVar, il));
                                    //GenerateStlocYReplacement(zVar.Index, methodDefinition.Body.GetILProcessor());
                                }
                            }

                            foreach (var targetInstruction in listStLdloc.Keys)
                            {
                                il.ReplaceInstructions(targetInstruction, listStLdloc[targetInstruction]);
                            }
                        }

                        methodDefinition.Body.OptimizeMacros();
                    }
                }
            }
        }

        private List<Instruction> GenerateLdlocXReplacement(VariableDefinition zVar, ILProcessor il)
        {
            List<Instruction> instructions = new List<Instruction>();
            instructions.Add(il.Create(OpCodes.Ldloc, zVar));
            instructions.Add(il.Create(OpCodes.Ldc_I4, 32));
            instructions.Add(il.Create(OpCodes.Shr));
            instructions.Add(il.Create(OpCodes.Conv_I4));
            return instructions;
        }

        private List<Instruction> GenerateLdlocYReplacement(VariableDefinition zVar, ILProcessor il)
        {
            List<Instruction> instructions = new List<Instruction>();
            instructions.Add(il.Create(OpCodes.Ldloc, zVar));
            instructions.Add(il.Create(OpCodes.Conv_I4));
            return instructions;
        }

        private List<Instruction> GenerateStlocYReplacement(VariableDefinition zVar, ILProcessor il)
        {
            List<Instruction> instructions = new List<Instruction>();
            instructions.Add(il.Create(OpCodes.Conv_I8));
            instructions.Add(il.Create(OpCodes.Ldloc, zVar));
            instructions.Add(il.Create(OpCodes.Ldc_I8, unchecked ((long) 0xFFFFFFFF00000000)));
            instructions.Add(il.Create(OpCodes.And));
            instructions.Add(il.Create(OpCodes.Or));
            instructions.Add(il.Create(OpCodes.Stloc, zVar));
            return instructions;
        }

        private List<Instruction> GenerateStlocXReplacement(VariableDefinition zVar, ILProcessor il)
        {
            List<Instruction> instructions = new List<Instruction>();
            instructions.Add(il.Create(OpCodes.Conv_I8));
            instructions.Add(il.Create(OpCodes.Ldc_I4, 32));
            instructions.Add(il.Create(OpCodes.Shl));
            instructions.Add(il.Create(OpCodes.Ldloc, zVar));
            instructions.Add(il.Create(OpCodes.Ldc_I4_M1));
            instructions.Add(il.Create(OpCodes.Conv_U8));
            instructions.Add(il.Create(OpCodes.And));
            instructions.Add(il.Create(OpCodes.Or));
            instructions.Add(il.Create(OpCodes.Stloc, zVar));
            return instructions;
        }
    }
}
