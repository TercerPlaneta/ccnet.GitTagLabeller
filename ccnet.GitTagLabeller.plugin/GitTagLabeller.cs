using System;
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
        public string Branch = "";

        /// <summary>
        /// Specifies an optional auto-increment of build or revision segments of the version
        /// </summary>
        /// <value>'none' (default), 'build', 'revision'</value>
        [ReflectorProperty("autoIncrement", Required = false)]
        public string AutoIncrement = "none";

        public override string Generate(IIntegrationResult integrationResult)
        {
            string workingDir = integrationResult.BaseFromWorkingDirectory(WorkingDirectory); 

            // if branch name specified, checkout to it
            if (!String.IsNullOrEmpty(Branch))
                GitExecute(workingDir, "checkout -q -f " + Branch);

            // get output from "git describe"
            string s = GitExecute(workingDir, "describe");
            if (s == null)
                throw new InvalidDataException("No tag found");

            string[] segm = s.Split('-');
            string tagName = segm[0];
            if (SkipPrefix.Length > 0 && tagName.StartsWith(SkipPrefix))
                tagName = tagName.Substring(SkipPrefix.Length);

            if (segm.Length == 1   // no commits ahead
                || (CommitCountAction.ToLower() == "ignore" && AutoIncrement == "none"))
            {
                Log.Debug("[gitTagLabeller] Label matches last Git tag: '{0}'", tagName);
                return tagName;    // return tag as label
            }
            int count;
            if (!int.TryParse(segm[1], out count))
                throw new InvalidDataException("Invalid return from Git describe");

            string countText = (count + CommitCountOffset).ToString();
            switch (CommitCountAction.ToLower())
            {
                case "concatenate":
                    tagName += "." + countText;
                    Log.Debug("[gitTagLabeller] Label after concatenating commit count on last Git tag: '{0}'", tagName);
                    break;

                case "ignore":
                    string[] vers = tagName.Split('.');
                    short major, minor, revision, build;
                    if (vers.Length != 4
                        || !short.TryParse(vers[0], out major)
                        || !short.TryParse(vers[1], out minor)
                        || !short.TryParse(vers[2], out build)
                        || !short.TryParse(vers[3], out revision))
                        throw new ArgumentException("May not use autoIncrement with version '" + tagName + "'. Verify last Git tag");
                    if (AutoIncrement == "revision")
                        ++revision;
                    else if (AutoIncrement == "build")
                    {
                        ++build;
                        revision = 0;
                    }
                    else
                        throw new ArgumentException("Invalid autoIncrement: " + AutoIncrement);

                    tagName = String.Format("{0}.{1}.{2}.{3}", major, minor, build, revision);
                    Log.Debug("[gitTagLabeller] Label after incrementing {1} in last Git tag: '{0}'", tagName, AutoIncrement);
                    break;

                case "replace":
                    int i = tagName.LastIndexOf('.');
                    tagName = (i < 0 ? tagName + "." : tagName.Substring(0, i + 1)) + countText;
                    Log.Debug("[gitTagLabeller] Label after replacing commit count in last Git tag: '{0}'", tagName);
                    break;

                default:
                    throw new ArgumentException("Invalid commitCountAction: " + CommitCountAction);
            }
            return tagName;

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