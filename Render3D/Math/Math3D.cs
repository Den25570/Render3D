using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using Render3D.Models;
using Render3D.Extensions;

namespace Render3D.Math
{
    public static class Math3D
    {
        public static Matrix4x4 GetViewportMatrix(float width, float height, float xmin, float ymin)
        {
            var matScale = Matrix4x4.Transpose(new Matrix4x4()
            {
                M11 = -width / 2,
                M22 = -height / 2,
                M33 = 1,
                M44 = 1
            });
            var matTransform = Matrix4x4.Transpose(new Matrix4x4()
            {
                M11 = 1,
                M22 = 1,
                M14 = xmin + width / 2,
                M24 = ymin + height / 2,
                M33 = 1,
                M44 = 1
            });
            return matScale * matTransform;
        }

        public static Matrix4x4 GetPerspectiveProjectionMatrix(float fov, float zNear, float zFar, float aspect)
        {
            var mat = Matrix4x4.Transpose(new Matrix4x4()
            {
                M11 = 1 / (aspect * MathF.Tan(fov / 2)),
                M22 = 1 / MathF.Tan(fov / 2),
                M33 = zFar / (zFar - zNear),
                M34 = -zFar * zNear / (zFar - zNear),
                M43 = 1
            });
            return mat;
        }

        public static Matrix4x4 GetInverseMatrix(Matrix4x4 mat)
        {
            Matrix4x4 result;
            Matrix4x4.Invert(mat, out result);
            return result;
        }

        public static Matrix4x4 GetLookAtMatrix(Vector3 eye, Vector3 target, Vector3 up)
        {
            var cameraDir = Vector3.Normalize(target - eye);
            var cameraRight = Vector3.Normalize(Vector3.Cross(up, cameraDir));
            var cameraUp = Vector3.Cross(cameraDir, cameraRight);

            var mat = Matrix4x4.Transpose(new Matrix4x4()
            {
                M11 = cameraRight.X,
                M12 = cameraRight.Y,
                M13 = cameraRight.Z,
                M21 = cameraUp.X,
                M22 = cameraUp.Y,
                M23 = cameraUp.Z,
                M31 = cameraDir.X,
                M32 = cameraDir.Y,
                M33 = cameraDir.Z,
                M44 = 1,
            });
            var positionMatrix = Matrix4x4.Transpose(new Matrix4x4()
            {
                M11 = 1,
                M14 = eye.X,
                M22 = 1,
                M24 = eye.Y,
                M33 = 1,
                M34 = eye.Z,
                M44 = 1,
            });

            return mat * positionMatrix;
        }

        public static Matrix4x4 GetTransformationMatrix(Vector3 scale, Vector3 rotation, Vector3 translation)
        {
            // scale - rotate - translate
            return GetScaleMatrix(scale) * GetRotationMatrix(rotation) * GetTranslationMatrix(translation);
        }

        public static Matrix4x4 GetRotationMatrix(Vector3 rotation)
        {
            var rotateMatrixX = Matrix4x4.Transpose(new Matrix4x4()
            {
                M11 = 1,
                M22 = (float)System.Math.Cos(rotation.X),
                M23 = -(float)System.Math.Sin(rotation.X),
                M32 = (float)System.Math.Sin(rotation.X),
                M33 = (float)System.Math.Cos(rotation.X),
                M44 = 1,
            });
            var rotateMatrixY = Matrix4x4.Transpose(new Matrix4x4()
            {
                M11 = (float)System.Math.Cos(rotation.Y),
                M13 = (float)System.Math.Sin(rotation.Y),
                M22 = 1,
                M31 = -(float)System.Math.Sin(rotation.Y),
                M33 = (float)System.Math.Cos(rotation.Y),
                M44 = 1,
            });
            var rotateMatrixZ = Matrix4x4.Transpose(new Matrix4x4()
            {
                M11 = (float)System.Math.Cos(rotation.Z),
                M12 = -(float)System.Math.Sin(rotation.Z),
                M21 = (float)System.Math.Sin(rotation.Z),
                M22 = (float)System.Math.Cos(rotation.Z),
                M33 = 1,
                M44 = 1,
            });
            return rotateMatrixX * rotateMatrixY * rotateMatrixZ;

            //return Matrix4x4.CreateRotationX(rotation.X) * Matrix4x4.CreateRotationX(rotation.Y) * Matrix4x4.CreateRotationX(rotation.Z);
        }

        public static Quaternion GetRotationWorldAngles(Vector3 forward, Quaternion rotation)
        {
            var rotationMatrix = GetRotationAroundVectorMatrix(forward, rotation);
            var quaternion = new Quaternion()
            {
                X = MathF.Atan2(rotationMatrix.M32, rotationMatrix.M33),
                Y = MathF.Atan2(-rotationMatrix.M31, MathF.Sqrt(rotationMatrix.M32 * rotationMatrix.M32 + rotationMatrix.M33 * rotationMatrix.M33)),
                Z = MathF.Atan2(rotationMatrix.M21, rotationMatrix.M11),
            };
            return quaternion;
        }

        public static Matrix4x4 GetRotationAroundVectorMatrix(Vector3 forward, Quaternion rotation)
        {
            forward = Vector3.Normalize(forward);
            var right = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, forward));
            var up = Vector3.Cross(forward, right);
            var rotateMatrixX = GetRodriguesRotationMatrix(right, rotation.X);
            var rotateMatrixY = GetRodriguesRotationMatrix(up, rotation.Y);
            var rotateMatrixZ = GetRodriguesRotationMatrix(forward, rotation.Z);
            return rotateMatrixX * rotateMatrixY * rotateMatrixZ;
        }

        public static Matrix4x4 GetScaleMatrix(Vector3 scale)
        {
            // scale rotate translate
            var scaleMatrix = new Matrix4x4()
            {
                M11 = scale.X,
                M22 = scale.Y,
                M33 = scale.Z,
                M44 = 1,
            };

            return Matrix4x4.Transpose(scaleMatrix);
        }

        public static Matrix4x4 GetTranslationMatrix(Vector3 translation)
        {
            // scale rotate translate
            var translateMatrix = Matrix4x4.Transpose(new Matrix4x4()
            {
                M11 = 1,
                M22 = 1,
                M33 = 1,
                M14 = translation.X,
                M24 = translation.Y,
                M34 = translation.Z,
                M44 = 1,
            });

            return translateMatrix;
        }

        public static Matrix4x4 GetRodriguesRotationMatrix(Vector3 v, float angle)
        {
            var w = GetWConstruct(v);
            var rotationMatrix = Matrix4x4.Identity + w * MathF.Sin(angle) + (w * w) * (2 * MathF.Sin(angle / 2) * MathF.Sin(angle / 2));
            return rotationMatrix;
        }

        public static Matrix4x4 GetWConstruct(Vector3 v)
        {
            return new Matrix4x4
            {
                M12 = -v.Z,
                M13 = v.Y,
                M21 = v.Z,
                M23 = -v.X,
                M31 = -v.Y,
                M32 = v.X,
                M44 = 1
            };
        }

        public static Matrix4x4 QuickInverse(Matrix4x4 m) // Only for Rotation/Translation Matrices
        {
            Matrix4x4 matrix = new Matrix4x4();
            matrix.M11 = m.M11; matrix.M12 = m.M21; matrix.M13 = m.M31; 
            matrix.M21 = m.M12; matrix.M22 = m.M22; matrix.M23 = m.M32;
            matrix.M31 = m.M13; matrix.M32 = m.M23; matrix.M33 = m.M33;
            matrix.M41 = -(m.M41 * matrix.M11 + m.M42 * matrix.M21 + m.M43 * matrix.M31);
            matrix.M42 = -(m.M41 * matrix.M12 + m.M42 * matrix.M22 + m.M43 * matrix.M32);
            matrix.M43 = -(m.M41 * matrix.M13 + m.M42 * matrix.M23 + m.M43 * matrix.M33);
            matrix.M44 = 1.0f;
            return matrix;
        }

        public static Vector3 LineIntersectionWithPlane(Vector3 plane, Vector3 planeNormal, Vector3 lineStart, Vector3 lineEnd)
        {
            planeNormal = Vector3.Normalize(planeNormal);
            float planeD = -Vector3.Dot(planeNormal, plane);
            float aD = Vector3.Dot(lineStart, planeNormal);
            float bD = Vector3.Dot(lineEnd, planeNormal);
            float t = (-planeD - aD) / (bD - aD);
            Vector3 lineStartToEnd = lineEnd - lineStart;
            Vector3 lineToIntersect = lineStartToEnd * t;
            return lineStart + lineToIntersect;
        }

        public static List<Triangle> ClipTriangle(Vector3 plane, Vector3 planeNormal, Triangle triangle)
        {
            planeNormal = Vector3.Normalize(planeNormal);
            Func<Vector4, float> shortestDistance = (Vector4 p) =>
            {
                Vector4 n = Vector4.Normalize(p);
                return (planeNormal.X * p.X + planeNormal.Y * p.Y + planeNormal.Z * p.Z - Vector3.Dot(planeNormal, plane));
            };

            List<Vector4> insidePoints = new List<Vector4>();
            List<Vector4> outsidePoints = new List<Vector4>(); ;

            float d0 = shortestDistance(triangle.Points[0]);
            float d1 = shortestDistance(triangle.Points[1]);
            float d2 = shortestDistance(triangle.Points[2]);

            if (d0 >= 0) { insidePoints.Add(triangle.Points[0]); }
            else { outsidePoints.Add(triangle.Points[0]); }
            if (d1 >= 0) { insidePoints.Add(triangle.Points[1]); }
            else { outsidePoints.Add(triangle.Points[1]); }
            if (d2 >= 0) { insidePoints.Add(triangle.Points[2]); }
            else { outsidePoints.Add(triangle.Points[2]); }

            List<Triangle> result = new List<Triangle> { };
            if (insidePoints.Count() == 3)
            {
                result.Add(triangle);
            }
            else if (insidePoints.Count() == 1 && outsidePoints.Count() == 2)
            {
                var newTriangle = new Triangle() { Normal = triangle.Normal };
                newTriangle.Points = new Vector4[]{
                    insidePoints[0],
                    new Vector4(LineIntersectionWithPlane(plane, planeNormal, insidePoints[0].ToVector3(), outsidePoints[0].ToVector3()), 1),
                    new Vector4(LineIntersectionWithPlane(plane, planeNormal, insidePoints[0].ToVector3(), outsidePoints[1].ToVector3()), 1)
                };
                result.Add(newTriangle);
            }
            else if (insidePoints.Count() == 2 && outsidePoints.Count() == 1)
            {
                var newTriangle1 = new Triangle() { Normal = triangle.Normal };
                var newTriangle2 = new Triangle() { Normal = triangle.Normal };

                newTriangle1.Points = new Vector4[]{
                    insidePoints[0],
                    insidePoints[1],
                    new Vector4(LineIntersectionWithPlane(plane, planeNormal, insidePoints[0].ToVector3(), outsidePoints[0].ToVector3()), 1)
                };
                newTriangle2.Points = new Vector4[]{
                    insidePoints[1],
                    newTriangle1.Points[2],
                    new Vector4(LineIntersectionWithPlane(plane, planeNormal, insidePoints[1].ToVector3(), outsidePoints[0].ToVector3()), 1)
                };
                result.Add(newTriangle1);
                result.Add(newTriangle2);
            }
            return result;
        }
    }
}
