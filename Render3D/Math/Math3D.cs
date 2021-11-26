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
        public static Matrix4x4 GetViewMatrix(Vector3 cameraPosition, Vector3 cameraRotation)
        {
            var viewTransform = GetTranslationMatrix(-cameraPosition);
            var rot = Quaternion.Inverse(Quaternion.CreateFromYawPitchRoll(cameraRotation.X, cameraRotation.Y, cameraRotation.Z));
            viewTransform = Matrix4x4.Transform(viewTransform, rot);
            return viewTransform;
        }

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
            result = QuickInverse(mat);
            return result;
        }

        public static Matrix4x4 GetLookAtMatrix(Vector3 eye, Vector3 target, Vector3 up, Vector3 right)
        {
            var front = Vector3.Normalize(target - eye);

            var mat = Matrix4x4.Transpose(new Matrix4x4()
            {
                M11 = right.X,
                M12 = right.Y,
                M13 = right.Z,
                M21 = up.X,
                M22 = up.Y,
                M23 = up.Z,
                M31 = front.X,
                M32 = front.Y,
                M33 = front.Z,
                M44 = 1,
            });
            var positionMatrix = Matrix4x4.Transpose(new Matrix4x4()
            {
                M11 = 1,
                M14 = -eye.X,
                M22 = 1,
                M24 = -eye.Y,
                M33 = 1,
                M34 = -eye.Z,
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

        public static Quaternion GetRotationWorldAngles(Vector3 forward, Vector3 rotation)
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

        public static Matrix4x4 GetRotationAroundVectorMatrix(Vector3 forward, Vector3 rotation)
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

        public static Vector3 RotateVectorAroundAxis(Vector3 v, Vector3 axis, float angle)
        {
            var vToAxis = Vector3.Multiply(Vector3.Multiply(v, axis), axis);
            var vOrthogonalToAxis = v - vToAxis;
            var w = Vector3.Cross(axis, v);
            var x1 = MathF.Cos(angle) / vOrthogonalToAxis.Length();
            var x2 = MathF.Sin(angle) / w.Length();
            var linearCombination = vOrthogonalToAxis.Length() * (vOrthogonalToAxis * x1 + w * x2);
            var result = linearCombination + vToAxis;
            return result;
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

        public static Vector3 LineIntersectionWithPlane(Vector3 plane, Vector3 planeNormal, Vector3 lineStart, Vector3 lineEnd, out float t)
        {
            planeNormal = Vector3.Normalize(planeNormal);
            float planeD = -Vector3.Dot(planeNormal, plane);
            float aD = Vector3.Dot(lineStart, planeNormal);
            float bD = Vector3.Dot(lineEnd, planeNormal);
            t = (-planeD - aD) / (bD - aD);
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

            List<int> insidePoints = new List<int>();
            List<int> outsidePoints = new List<int>();
            (shortestDistance(triangle.Points[0]) >= 0 ? insidePoints : outsidePoints).Add(0);
            (shortestDistance(triangle.Points[1]) >= 0 ? insidePoints : outsidePoints).Add(1);
            (shortestDistance(triangle.Points[2]) >= 0 ? insidePoints : outsidePoints).Add(2);

            List<Triangle> result = new List<Triangle> { };
            if (insidePoints.Count() == 3)
            {
                result.Add(triangle);
            }
            else if (insidePoints.Count() == 1 && outsidePoints.Count() == 2)
            {
                var newTriangle = new Triangle(triangle);

                float t1, t2;
                newTriangle.Points = new Vector4[]{
                    triangle.Points[insidePoints[0]],
                    new Vector4(LineIntersectionWithPlane(plane, planeNormal, triangle.Points[insidePoints[0]].ToVector3(), triangle.Points[outsidePoints[0]].ToVector3(), out t1), 1),
                    new Vector4(LineIntersectionWithPlane(plane, planeNormal, triangle.Points[insidePoints[0]].ToVector3(), triangle.Points[outsidePoints[1]].ToVector3(), out t2), 1)
                };
                newTriangle.Normals = new Vector3[]{
                    triangle.Normals[insidePoints[0]],
                    triangle.Normals[insidePoints[0]] + t1 * (triangle.Normals[outsidePoints[0]] - triangle.Normals[insidePoints[0]]),
                    triangle.Normals[insidePoints[0]] + t2 * (triangle.Normals[outsidePoints[1]] - triangle.Normals[insidePoints[0]]),
                };
                newTriangle.Colors = new Vector3[]{
                    triangle.Colors[insidePoints[0]],
                    triangle.Colors[insidePoints[0]] + t1 * (triangle.Colors[outsidePoints[0]] - triangle.Colors[insidePoints[0]]),
                    triangle.Colors[insidePoints[0]] + t2 * (triangle.Colors[outsidePoints[1]] - triangle.Colors[insidePoints[0]]),
                };
                newTriangle.TextureCoordinates = new Vector3[]{
                    triangle.TextureCoordinates[insidePoints[0]],
                    triangle.TextureCoordinates[insidePoints[0]] + t1 * (triangle.TextureCoordinates[outsidePoints[0]] - triangle.TextureCoordinates[insidePoints[0]]),
                    triangle.TextureCoordinates[insidePoints[0]] + t2 * (triangle.TextureCoordinates[outsidePoints[1]] - triangle.TextureCoordinates[insidePoints[0]]),
                };
                result.Add(newTriangle);
            }
            else if (insidePoints.Count() == 2 && outsidePoints.Count() == 1)
            {
                //TODO: Interpolate normal
                var newTriangle1 = new Triangle(triangle);
                var newTriangle2 = new Triangle(triangle);

                float t;
                newTriangle1.Points = new Vector4[]{
                    triangle.Points[insidePoints[0]],
                    triangle.Points[insidePoints[1]],
                    new Vector4(LineIntersectionWithPlane(plane, planeNormal, triangle.Points[insidePoints[0]].ToVector3(), triangle.Points[outsidePoints[0]].ToVector3(), out t), 1)
                };
                newTriangle1.Colors = new Vector3[]{
                    triangle.Colors[insidePoints[0]],
                    triangle.Colors[insidePoints[1]],
                    triangle.Colors[insidePoints[0]] + t * (triangle.Colors[outsidePoints[0]] - triangle.Colors[insidePoints[0]]),
                };
                newTriangle1.Normals = new Vector3[]{
                    triangle.Normals[insidePoints[0]],
                    triangle.Normals[insidePoints[1]],
                    triangle.Normals[insidePoints[0]] + t * (triangle.Normals[outsidePoints[0]] - triangle.Normals[insidePoints[0]]),
                };
                newTriangle1.TextureCoordinates = new Vector3[]{
                    triangle.TextureCoordinates[insidePoints[0]],
                    triangle.TextureCoordinates[insidePoints[1]],
                    triangle.TextureCoordinates[insidePoints[0]] + t * (triangle.TextureCoordinates[outsidePoints[0]] - triangle.TextureCoordinates[insidePoints[0]]),
                };

                newTriangle2.Points = new Vector4[]{
                    triangle.Points[insidePoints[1]],
                    newTriangle1.Points[2],
                    new Vector4(LineIntersectionWithPlane(plane, planeNormal, triangle.Points[insidePoints[1]].ToVector3(), triangle.Points[outsidePoints[0]].ToVector3(), out t), 1)
                };
                newTriangle2.Normals = new Vector3[]{
                    triangle.Normals[insidePoints[1]],
                    newTriangle1.Normals[2],
                    triangle.Normals[insidePoints[1]] + t * (triangle.Normals[outsidePoints[0]] - triangle.Normals[insidePoints[1]]),
                };
                newTriangle2.Colors = new Vector3[]{
                    triangle.Colors[insidePoints[1]],
                    newTriangle1.Colors[2],
                    triangle.Colors[insidePoints[1]] + t * (triangle.Colors[outsidePoints[0]] - triangle.Colors[insidePoints[1]]),
                };
                newTriangle2.TextureCoordinates = new Vector3[]{
                    triangle.TextureCoordinates[insidePoints[1]],
                    newTriangle1.TextureCoordinates[2],
                    triangle.TextureCoordinates[insidePoints[1]] + t * (triangle.TextureCoordinates[outsidePoints[0]] - triangle.TextureCoordinates[insidePoints[1]]),
                };
                result.Add(newTriangle1);
                result.Add(newTriangle2);
            }
            return result;
        }
        public static float InterpolateZ(Vector4 v1, Vector4 v2, Vector4 v3, float x, float y)
        {
            var W1 = ((v2.Y - v3.Y) * (x - v3.X) + (v3.X - v2.X) * (y - v3.Y)) / ((v2.Y - v3.Y) * (v1.X - v3.X) + (v3.X - v2.X) * (v1.Y - v3.Y));
            var W2 = ((v3.Y - v1.Y) * (x - v3.X) + (v1.X - v3.X) * (y - v3.Y)) / ((v2.Y - v3.Y) * (v1.X - v3.X) + (v3.X - v2.X) * (v1.Y - v3.Y));
            var W3 = 1 - W1 - W2;
            return v1.Z * W1 + v2.Z * W2 + v3.Z * W3;
        }

        public static Vector3 GetBarycenter(Vector4 v1, Vector4 v2, Vector4 v3, float x, float y)
        {
            var div = 1 / ((v2.Y - v3.Y) * (v1.X - v3.X) + (v3.X - v2.X) * (v1.Y - v3.Y));
            var W1 = ((v2.Y - v3.Y) * (x - v3.X) + (v3.X - v2.X) * (y - v3.Y)) * div;
            var W2 = ((v3.Y - v1.Y) * (x - v3.X) + (v1.X - v3.X) * (y - v3.Y)) * div;
            var W3 = 1 - W1 - W2;
            return new Vector3(W1, W2, W3);
        }

        public static Matrix4x4 GetTriangleTBNMatrix(Triangle tri, Vector3 normal)
        {
            Vector3 edge1 = (tri.Points[1] - tri.Points[0]).ToVector3();
            Vector3 edge2 = (tri.Points[2] - tri.Points[0]).ToVector3();
            Vector3 deltaUV1 = (tri.TextureCoordinates[1] - tri.TextureCoordinates[0]);
            Vector3 deltaUV2 = (tri.TextureCoordinates[2] - tri.TextureCoordinates[0]);

            float f = 1.0f / (deltaUV1.X * deltaUV2.Y - deltaUV2.X * deltaUV1.Y);
            var tangent = Vector3.Normalize(new Vector3(
                f * (deltaUV2.Y * edge1.X - deltaUV1.Y * edge2.X),
                f * (deltaUV2.Y * edge1.Y - deltaUV1.Y * edge2.Y),
                f * (deltaUV2.Y * edge1.Z - deltaUV1.Y * edge2.Z)));
            var biTangent = Vector3.Normalize(new Vector3(
                f * (-deltaUV2.X * edge1.X + deltaUV1.X * edge2.X),
                f * (-deltaUV2.X * edge1.Y + deltaUV1.X * edge2.Y),
                f * (-deltaUV2.X * edge1.Z + deltaUV1.X * edge2.Z)));

            return new Matrix4x4(
                tangent.X, tangent.Y, tangent.Z, 0,
                biTangent.X, biTangent.Y, biTangent.Z, 0,
                normal.X, normal.Y, normal.Z, 0,
                0,0,0,1);
        }

        public static Matrix4x4 GetLightViewMatrix(Vector3 lightPos, Vector3 modelCenter, float zNear, float zFar)
        {
            Matrix4x4 lightProjection = Matrix4x4.CreateOrthographicOffCenter(-10.0f, 10.0f, -10.0f, 10.0f, zNear, zFar);
            Matrix4x4 lightView = Matrix4x4.CreateLookAt(lightPos, modelCenter, Vector3.UnitY);
            return lightView * lightProjection;
        }
    }
}
