//Copyright (c) 2024 Jefferson University
//Copyright (c) 2024 Bruska Technologies LLC
//Copyright (c) 2023 University of Pennsylvania

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

using System.Numerics;

namespace UnityEPL {

    public class Statistics {
        // https://stackoverflow.com/a/51740258
        public static BigInteger Factorial(BigInteger integer) {
            if(integer < 1) return new BigInteger(1);

            BigInteger result = integer;
            for (BigInteger i = 1; i < integer; i++)
            {
                result = result * i;
            }

            return result;
        }

        // https://stackoverflow.com/a/51740258
        public static BigInteger Permutation(BigInteger n, BigInteger r) {
            return Factorial(n) / Factorial(n-r);
        }

        public static BigInteger Combination(BigInteger n, BigInteger r) {
            return Permutation(n, r) / Factorial(r);
        }
    }
}