# Git tag labeller plugin for CCNET  #

This labeller plugin builds the CCNet internal label from the last tag on a git repository branch - usually downloaded using the git source control provider.

As any other CCNet labeller, used in conjunction with the AssemblyInfo task in  [MsBuildTasks](https://github.com/loresoft/msbuildtasks) or with a manual script it allows setting the AssemblyVersion - AssemblyFileVersion attributes of an MSBuild project.


## Building the label ##

The resulting label is built from the output of this command:
	
	git describe

If the last commit is the one pointed by the tag, the returned label is just the tag name - without the optional prefix, see below.

Otherwise, the number of commits ahead the tag is combined with the tag name to build the label. Several options are available, configuring the *commitCountAction* element (see example below)

* **concatenate** - a dot is added to the tag name, followed by the commit count.
* **replace** - the last part of the tag (following the last dot) is replaced by the commit count.
* **ignore** - the commit count is not used, the last tag name is always used as label.

If *concatenate* or *replace* are used, an optional *commitCountOffset* may be specified

In all cases:

* if a *skipPrefix* value is provided, that prefix is extracted from the first part of the tag name.
* if a *branch* value is provided, the specified branch is checked out before building the label with *git describe*. This is useful when working with 'release' branches that have their own tags

## Examples ##

Example 1:

* code is 3 commits ahead of 'ReleaseCandidate' tag
* commitCountAction is configured as '*concatenate*'
* ==> the resulting label is 'ReleaseCandidate.3'

Example 2:

* code is 3 commits ahead of 'v1.0.12.0' tag
* skipPrefix is configured as 'v'
* commitCountAction is configured as '*replace*'
* commitCountOffset is configured as 100
* ==> the resulting label is '1.0.12.103'

This allows for handling both QA commits (having no tag) and tagged releases, as suggested in this post: [Get Going with a Minimalistic Git Workflow](http://pampanotes.tercerplaneta.com/2012/07/get-going-with-minimalistic-git.html)

## Usage ##

	<labeller type="gitTagLabeller">
		<workingDirectory>my_git_folder</workingDirectory>
		<commitCountAction>replace</commitCountAction>
		<commitCountOffset>100</commitCountOffset>
		<skipPrefix>my_label_prefix</skipPrefix>
		<executable>path\git.exe</executable>
		<dynamicValues>.....</dynamicValues>
	</labeller>

* *commitCountOffset* is optional (defaults to 0)
* *skipPrefix* is optional 
* *executable* is optional (by default searchs in PATH)
* *dynamicValues*: starting with version 1.0.1 of this plugin, [dynamic values](http://build.sharpdevelop.net/ccnet/doc/CCNET/Dynamic%20Parameters.html) may be used either explicitly or by implied replacement.

## How to make use of the label	

* In an MSBuild target you may access the value as $(CCNetLabel)
* In NAnt you may use $[CCNetLabel]
* To use the value inside the CCNet config blocks, you need to write it as a dynamic parameter, i.e. $[$CCNetLabel] . This is valid only for CCNet 1.5+

## Installation ##

* Before building, update the *ThoughtWorks.CruiseControl.Core.dll* and *ThoughtWorks.CruiseControl.Remote.dll* in the *lib* folder with the ones corresponding to your CCNET version (found in *server* subfolder of the CruiseControl.Net program files folder)

* Build the solution

* Copy the resulting *ccnet.GitTagLabeller.plugin.dll* into the *server* subfolder.

NOTE: the assembly file name must follow the ccnet.*.plugin.dll pattern - in order to be loaded by CCNET

### Credits ###

The plumbing for the plugin code (Visual Studio project and interfaces to implement) was taken from Matthias Hurrle [CCNet Git Revision Labeller plugin](https://github.com/atzedent/ccnet.GitRevisionLabeller.plugin). Thanks!

