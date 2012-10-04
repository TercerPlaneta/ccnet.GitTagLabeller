﻿using System;
using System.IO;
using System.Text;
using Exortech.NetReflector;
using ThoughtWorks.CruiseControl.Core;
using ThoughtWorks.CruiseControl.Core.Util;
using ThoughtWorks.CruiseControl.Remote;
using ThoughtWorks.CruiseControl.Core.Label;

namespace CcNet.Labeller
{
    /// <summary>
    /// Builds a CCNET Label based on the last tag and further commit count as returned by 'git describe'
    /// The result is available via the <c>$(CCNetLabel)</c> property
    /// </summary>
    [ReflectorType ("gitTagLabeller")]
    public class GitTagLabeller : LabellerBase, ILabeller
    {
        /// <summary>
        /// Gets or sets the path to the Git working directory.
        /// </summary>
        /// <value>The working directory.</value>
        [ReflectorProperty ("workingDirectory", Required = true)]
        public string WorkingDirectory;

        /// <summary>
        /// Gets or sets a value indicating how to combine the commit count with the last label
        /// </summary>
        /// <value><c>concatenate</c>, <c>replace</c> or <c>ignore</c>.</value>
        [ReflectorProperty ("commitCountAction", Required = true)]
        public string CommitCountAction { get; set; }

        /// <summary>
        /// Gets or sets a value indicating an offset to add to the commit count, when not zero
        /// </summary>
        /// <value><c>concatenate</c>, <c>replace</c> or <c>ignore</c>.</value>
        [ReflectorProperty("commitCountOffset", Required = false)]
        public int CommitCountOffset { get; set; }

        /// <summary>
        /// Gets or sets a prefix to skip from the tag name, i.e. "v" to get label "1.0.0.0" from tag "v1.0.0.0"
        /// </summary>
        /// <value>Prefix to exclude from the generated label.</value>
        [ReflectorProperty("skipPrefix", Required = false)]
        public string SkipPrefix = "";

        /// <summary>
        /// Gets or sets the path to the Git executable.
        /// </summary>
        /// <remarks>
        /// By default, the labeller checks the <c>PATH</c> environment variable.
        /// </remarks>
        /// <value>The executable.</value>
        [ReflectorProperty("executable", Required = false)]
        public string Executable = "git";

        /// <summary>
        /// Gets or sets a branch name to checkout before getting the label
        /// </summary>
        /// <value>Branch Name</value>
        [ReflectorProperty("branch", Required = false)]
        public string branch = "";

        public override string Generate(IIntegrationResult integrationResult)
        {
            string workingDir = integrationResult.BaseFromWorkingDirectory(WorkingDirectory); 

            // if branch name specified, checkout to it
            if (!String.IsNullOrEmpty(branch))
                GitExecute(workingDir, "checkout -q -f " + branch);

            // get output from "git describe"
            string s = GitExecute(workingDir, "describe");
            if (s == null)
                throw new InvalidDataException("No tag found");

            string[] segm = s.Split('-');
            string tagName = segm[0];
            if (SkipPrefix.Length > 0 && tagName.StartsWith(SkipPrefix))
                tagName = tagName.Substring(SkipPrefix.Length);

            if (segm.Length == 1)  // no commits ahead
                return tagName;    // return tag as label
            int count;
            if (!int.TryParse(segm[1], out count))
                throw new InvalidDataException("Invalid return from Git describe");

            string countText = (count + CommitCountOffset).ToString();
            switch (CommitCountAction.ToLower())
            {
                case "concatenate":
                    return tagName + "." + countText;

                case "ignore":
                    return tagName;

                case "replace":
                    int i = tagName.LastIndexOf('.');
                    return (i<0? tagName+"." : tagName.Substring(0, i+1)) + countText;
            }
            throw new ArgumentException("Invalid commitCountAction: " + CommitCountAction);
 
        }

        private string GitExecute(string workingDir, string args)
        {
            string s;
            var processInfo = new ProcessInfo(Executable, args, workingDir);
            processInfo.StreamEncoding = Encoding.UTF8;

            Log.Info("Execute: git " + args);

            s = (new ProcessExecutor()).Execute(processInfo).StandardOutput;
            return s==null? s: s.Trim();
        }

    }
}