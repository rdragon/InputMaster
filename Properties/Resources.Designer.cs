﻿//------------------------------------------------------------------------------
// <auto-generated>
//   This code was generated by a tool.
//   Runtime Version:4.0.30319.42000
//
//   Changes to this file may cause incorrect behavior and will be lost if
//   the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace InputMaster.Properties {
  using System;
  
  
  /// <summary>
  ///   A strongly-typed resource class, for looking up localized strings, etc.
  /// </summary>
  // This class was auto-generated by the StronglyTypedResourceBuilder
  // class via a tool like ResGen or Visual Studio.
  // To add or remove a member, edit your .ResX file then rerun ResGen
  // with the /str option, or rebuild your VS project.
  [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
  [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
  internal class Resources {
    
    private static global::System.Resources.ResourceManager resourceMan;
    
    private static global::System.Globalization.CultureInfo resourceCulture;
    
    [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
    internal Resources() {
    }
    
    /// <summary>
    ///   Returns the cached ResourceManager instance used by this class.
    /// </summary>
    [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
    internal static global::System.Resources.ResourceManager ResourceManager {
      get {
        if (object.ReferenceEquals(resourceMan, null)) {
          global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("InputMaster.Properties.Resources", typeof(Resources).Assembly);
          resourceMan = temp;
        }
        return resourceMan;
      }
    }
    
    /// <summary>
    ///   Overrides the current thread's CurrentUICulture property for all
    ///   resource lookups using this strongly typed resource class.
    /// </summary>
    [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
    internal static global::System.Globalization.CultureInfo Culture {
      get {
        return resourceCulture;
      }
      set {
        resourceCulture = value;
      }
    }
    
    /// <summary>
    ///   Looks up a localized resource of type System.Drawing.Icon similar to (Icon).
    /// </summary>
    internal static System.Drawing.Icon NotifyIcon {
      get {
        object obj = ResourceManager.GetObject("NotifyIcon", resourceCulture);
        return ((System.Drawing.Icon)(obj));
      }
    }
    
    /// <summary>
    ///   Looks up a localized string similar to ::Test a a  → Typing a should output a.
    ///::Test abc abc  → Typing the three letters a, b and c should output abc.
    ///::Test {Hold}a{Release}a a  → Check if {Hold} and {Release} are correctly being parsed. The token {Hold} specifies that only the key down event should be simulated ({Release} specifies the key up event).
    ///::Test {Hold}{LShift}a{Release}{LShift} A
    ///::Test {Hold}{RShift}ab{Release}{RShift}c ABc
    ///::Test {Shift}a A  → Check if the modifier {Shift} is working. {Shift}a is another way to write {Hold} [rest of string was truncated]&quot;;.
    /// </summary>
    internal static string Tests {
      get {
        return ResourceManager.GetString("Tests", resourceCulture);
      }
    }
  }
}
