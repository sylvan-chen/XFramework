using UnityEngine;

namespace XFramework
{
    public class IntDropdownAttribute : PropertyAttribute
    {
        public int[] Options;

        public IntDropdownAttribute(params int[] options)
        {
            Options = options;
        }
    }
}