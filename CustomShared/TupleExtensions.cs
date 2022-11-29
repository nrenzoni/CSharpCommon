using System.Collections;
using System.Runtime.CompilerServices;

namespace CustomShared;

public static class TupleExtensions
{
    public static bool NoNullMembers(this ITuple tuple)
    {
        for (var i = 0; i < tuple.Length; ++i)
        {
            if (tuple[i] is null)
                return false;
        }

        return true;
    }
}