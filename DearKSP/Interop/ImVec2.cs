namespace DearKSP.Interop
{
    /// <summary>
    /// Blittable mirror of cimgui's <c>ImVec2_c</c> (<c>struct { float x, y; }</c>, cimgui.h:260).
    /// 8 bytes, passed by value to <c>igButton</c> — safe on Win64 Cdecl.
    /// </summary>
    internal struct ImVec2
    {
        public float X;
        public float Y;

        public ImVec2(float x, float y)
        {
            X = x;
            Y = y;
        }
    }
}
