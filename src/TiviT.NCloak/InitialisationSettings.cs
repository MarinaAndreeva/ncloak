﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace TiviT.NCloak
{
    public class InitialisationSettings
    {
        private readonly List<string> assembliesToObfuscate;
        private string tamperProofAssemblyName;
        private bool validated;

        /// <summary>
        /// Initializes a new instance of the <see cref="InitialisationSettings"/> class.
        /// </summary>
        public InitialisationSettings()
        {
            assembliesToObfuscate = new List<string>();
            validated = false;
            SupressIldasm = true;
        }

        
        /// <summary>
        /// Gets a list of the assemblies to obfuscate.
        /// </summary>
        /// <value>The assemblies to obfuscate.</value>
        public List<string> AssembliesToObfuscate
        {
            get
            {
                return assembliesToObfuscate;
            }
        }

        /// <summary>
        /// Gets or sets the output directory.
        /// </summary>
        /// <value>The output directory.</value>
        public string OutputDirectory { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the obfuscator should obfuscate all access modifiers as opposed
        /// to just private access modifiers
        /// </summary>
        /// <value>
        /// 	<c>true</c> to obfuscate all modifiers; otherwise, <c>false</c>.
        /// </value>
        public bool ObfuscateAllModifiers
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the obfuscator should encrypt strings.
        /// </summary>
        /// <value><c>true</c> to encrypt strings; otherwise, <c>false</c>.</value>
        public bool EncryptStrings
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to include the SupressIldasmAttribute on the assembly
        /// </summary>
        /// <value><c>true</c> to include the attribute; otherwise, <c>false</c>.</value>
        public bool SupressIldasm
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the method used to confuse decompilation tools.
        /// </summary>
        /// <value>The method used to confuse decompilation tools.</value>
        public ConfusionMethod ConfuseDecompilationMethod
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the name of the tamper proof assembly. Please note, this
        /// is the .NET friendly name
        /// </summary>
        /// <value>The name of the tamper proof assembly.</value>
        public string TamperProofAssemblyName
        {
            get { return tamperProofAssemblyName; }
            set
            {
                //Validate it
                if (String.IsNullOrEmpty(value))
                    tamperProofAssemblyName = null;
                else if (Regex.IsMatch(value, "[A-Za-z][_A-Za-z0-9]*"))
                    tamperProofAssemblyName = value;
                else
                    throw new FormatException("Assembly name must be a valid .NET friendly type name ([A-Za-z][_A-Za-z0-9]*)");
            }
        }

        /// <summary>
        /// Gets or sets the type of the tamper proof assembly.
        /// </summary>
        /// <value>The type of the tamper proof assembly.</value>
        public AssemblyType TamperProofAssemblyType
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether rename is switched OFF.
        /// </summary>
        /// <value><c>true</c> if rename switched OFF; otherwise, <c>false</c>.</value>
        public bool NoRename
        {
            get; set;
        }

        public bool CombineLocals
        {
            get; set;
        }

        /// <summary>
        /// Validates the initialisation settings.
        /// </summary>
        public void Validate()
        {
            //Only validate if it hasn't already
            if (validated)
                return;

            //Check the assemblies to load
            if (assembliesToObfuscate.Count == 0)
                throw new InitialisationException("Must specify at least one assembly to obfuscate");
            
            //Make sure each file exists and that it is a valid assembly
            foreach (string assembly in assembliesToObfuscate)
            {
                //Check it exists
                if (!File.Exists(assembly))
                    throw new InitialisationException(String.Format("The specified assembly \"{0}\" does not exist", assembly));

                //Check it's a valid assembly
                try
                {
                    AssemblyName.GetAssemblyName(assembly);
                }
                catch (Exception ex)
                {
                    throw new InitialisationException(String.Format("The specified assembly \"{0}\" is not valid.", assembly), ex);
                }
            }

            //Check the output directory
            if (String.IsNullOrEmpty(OutputDirectory))
                throw new InitialisationException("An output directory is required");
            if (!Directory.Exists(OutputDirectory))
                throw new InitialisationException(String.Format("The output directory {0} does not currently exist.", OutputDirectory));

            //Set it to validated
            validated = true;
        }
    }
}
