// We have our own Quaternion class because Unity's Quaternion is more suited for rotations.
// We care about quaternion arithmetic, which is straightforward enough to program.

namespace KtaneQuaternions
{
    public struct Quaternion
    {
        public static readonly Quaternion I = new Quaternion(0, 1, 0, 0);
        public static readonly Quaternion J = new Quaternion(0, 0, 1, 0);
        public static readonly Quaternion K = new Quaternion(0, 0, 0, 1);

        public readonly int A;
        public readonly int B;
        public readonly int C;
        public readonly int D;

        public Quaternion(int a, int b, int c, int d)
        {
            A = a;
            B = b;
            C = c;
            D = d;
        }

        public static implicit operator Quaternion(int n)
        {
            return new Quaternion(n, 0, 0, 0);
        }

        public static Quaternion operator +(Quaternion q)
        {
            return q * 1;
        }

        public static Quaternion operator -(Quaternion q)
        {
            return q * -1;
        }

        public static Quaternion operator +(Quaternion q1, Quaternion q2)
        {
            return new Quaternion(q1.A + q2.A, q1.B + q2.B, q1.C + q2.C, q1.D + q2.D);
        }

        public static Quaternion operator -(Quaternion q1, Quaternion q2)
        {
            return q1 + -q2;
        }

        public static Quaternion operator *(Quaternion q1, Quaternion q2)
        {
            return new Quaternion(
                q1.A*q2.A - q1.B*q2.B - q1.C*q2.C - q1.D*q2.D,
                q1.A*q2.B + q1.B*q2.A + q1.C*q2.D - q1.D*q2.C,
                q1.A*q2.C - q1.B*q2.D + q1.C*q2.A + q1.D*q2.B,
                q1.A*q2.D + q1.B*q2.C - q1.C*q2.B + q1.D*q2.A
            );
        }

        public static bool operator ==(Quaternion q1, Quaternion q2)
        {
            return q1.A == q2.A && q1.B == q2.B && q1.C == q2.C && q1.D == q2.D;
        }

        public static bool operator !=(Quaternion q1, Quaternion q2)
        {
            return !(q1 == q2);
        }

        public Quaternion Conjugate()
        {
            return new Quaternion(A, -B, -C, -D);
        }

        public override bool Equals(object obj)
        {
            return obj != null && GetType().Equals(obj.GetType()) && this == (Quaternion) obj;
        }

        public override int GetHashCode()
        {
            uint uA = (uint) A;
            uint uB = (uint) B;
            uint uC = (uint) C;
            uint uD = (uint) D;
            return (int) (uA ^ (uB << 8 | uB >> 24) ^ (uC << 16 | uC >> 16) ^ (uD << 24 | uD >> 8));
        }

        public int SquaredNorm()
        {
            return A*A + B*B + C*C + D*D;
        }

        public override string ToString()
        {
            return string.Format("{0}{1:+#;-#;+0}i{2:+#;-#;+0}j{3:+#;-#;+0}k", A, B, C, D);
        }
    }
}