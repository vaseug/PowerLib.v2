namespace PowerLib.System;

public static class Variable
{
  public static T? Replace<T>(ref T? variable, T? value)
  {
    var result = variable;
    variable = value;
    return result;
  }

  public static T? Take<T>(ref T? variable)
    => Replace(ref variable, default);
}
