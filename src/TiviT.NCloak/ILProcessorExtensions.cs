using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Cil;

namespace TiviT.NCloak.CloakTasks
{
    public static class ILProcessorExtensions
    {
        public static void InsertBeforeInstructions(this ILProcessor il, Instruction target,
            ICollection<Instruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                il.InsertBefore(target, instruction); 
            }
        }

        public static void InsertAfterInstructions(this ILProcessor il, Instruction target,
            ICollection<Instruction> instructions)
        {
            foreach (var instruction in instructions.Reverse())
            {
                il.InsertAfter(target, instruction);
            }
        }

        public static void ReplaceInstructions(this ILProcessor il, Instruction target,
            ICollection<Instruction> instructions)
        {
            il.InsertAfterInstructions(target, instructions);
            foreach (var currInstruction in il.Body.Instructions)
            {
                if (currInstruction.Operand == target)
                {
                    currInstruction.Operand = instructions.First();
                }
            }
            il.Remove(target);
            //il.Replace(target, il.Create(OpCodes.Nop));
        }
    }
}