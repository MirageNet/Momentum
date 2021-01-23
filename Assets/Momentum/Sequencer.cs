/*
The MIT License (MIT)
Copyright (c) 2020 Fredrik Holmstrom
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

namespace Mirror.Momentum
{
    public struct Sequencer
    {
        int _shift;
        int _bytes;

        ulong _mask;
        ulong _sequence;

        public int Bytes => _bytes;

        public Sequencer(int bytes)
        {
            // 1 byte
            // (1 << 8) = 256
            // - 1      = 255
            //          = 1111 1111

            _bytes = bytes;
            _sequence = 0;
            _mask = (1UL << (bytes * 8)) - 1UL;
            _shift = (sizeof(ulong) - bytes) * 8;
        }

        public ulong Next()
        {
            return _sequence = NextAfter(_sequence);
        }

        public ulong NextAfter(ulong sequence)
        {
            return (sequence + 1UL) & _mask;
        }

        public long Distance(ulong from, ulong to)
        {
            to <<= _shift;
            from <<= _shift;
            return ((long)(from - to)) >> _shift;
        }

        // 0 1 2 3 4 5 6 7 8 9 ... 255
        // wraps around back to 0

    }
}