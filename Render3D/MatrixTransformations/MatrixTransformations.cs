using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using Render3D.Model;

namespace Render3D.MatrixTransformation
{
    public static class MatrixTransformations
    {
        public static Matrix4x4 GetFinalMatrix(Matrix4x4 viewport, Matrix4x4 projection, Matrix4x4 view, Matrix4x4 model)
        {
            //var result = Matrix4x4.Multiply(viewport, projection);
            var result = projection;
            result = Matrix4x4.Multiply(result, view);
            result = Matrix4x4.Multiply(result, model);
            return result;
        }

        public static Matrix4x4 GetTransformationMatrixViewport(float width, float height, float xmin, float ymin)
        {
            var mat = Matrix4x4.Transpose(new Matrix4x4()
            {
                M11 = width / 2,
                M22 = -height / 2,
                M14 = xmin + width / 2,
                M24 = ymin + height / 2,
                M33 = 1,
                M44 = 1
            });
            return mat;
        }

        public static Matrix4x4 GetTransformationMatrixPerspectiveProjection(float fov, float zNear, float zFar, float aspect)
        {
            var mat = Matrix4x4.Transpose(new Matrix4x4()
            {
                M11 = 1 / (aspect * MathF.Tan(fov / 2)),
                M22 = 1 / MathF.Tan(fov / 2),
                M33 = zFar / (zNear - zFar),
                M34 = zFar * zNear / (zNear - zFar),
                M43 = -1
            });
            return mat;
        }

        public static Matrix4x4 GetTransformationMatrixByCamera(Vector3 eye, Vector3 target, Vector3 up)
        {
            var cameraDir = Vector3.Normalize(eye - target);
            var cameraRight = Vector3.Normalize(Vector3.Cross(up, cameraDir));
            var cameraUp = Vector3.Cross(cameraDir, cameraRight);

            var mat = Matrix4x4.Transpose(new Matrix4x4()
            {
                M11 = cameraRight.X, M12 = cameraRight.Y, M13 = cameraRight.Z,
                M21 = cameraUp.X, M22 = cameraUp.Y, M23 = cameraUp.Z,
                M31 = cameraDir.X, M32 = cameraDir.Y, M33 = cameraDir.Z,
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

        public static Matrix4x4 GetTransformationMatrix(Vector3 scale, Quaternion rotation, Vector3 translation)
        {
            // scale - rotate - translate
            return GetTranslationMatrix(translation) * GetRotationMatrix(rotation) * GetScaleMatrix(scale);
        }

        public static Matrix4x4 GetRotationMatrix(Quaternion rotation)
        {
            var rotateMatrixX = Matrix4x4.Transpose(new Matrix4x4()
            {
                M11 = 1,
                M22 = (float)Math.Cos(rotation.X),
                M23 = -(float)Math.Sin(rotation.X),
                M32 = (float)Math.Sin(rotation.X),
                M33 = (float)Math.Cos(rotation.X),
                M44 = 1,
            });
            var rotateMatrixY = Matrix4x4.Transpose(new Matrix4x4()
            {
                M11 = (float)Math.Cos(rotation.Y),
                M13 = (float)Math.Sin(rotation.Y),
                M22 = 1,
                M31 = -(float)Math.Sin(rotation.Y),
                M33 = (float)Math.Cos(rotation.Y),
                M44 = 1,
            });
            var rotateMatrixZ = Matrix4x4.Transpose(new Matrix4x4()
            {
                M11 = (float)Math.Cos(rotation.Z),
                M12 = -(float)Math.Sin(rotation.Z),
                M21 = (float)Math.Sin(rotation.Z),
                M22 = (float)Math.Cos(rotation.Z),
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
                Y = MathF.Atan2(-rotationMatrix.M31, rotationMatrix.M33),
                Z = MathF.Atan2(rotationMatrix.M21, rotationMatrix.M11),
            };
            return quaternion;
        }

        public static Matrix4x4 GetRotationAroundVectorMatrix(Vector3 forward, Quaternion rotation)
        {
            forward = Vector3.Normalize(forward);
            var right = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, forward));
            var up = Vector3.Cross(forward, right);
            var rotateMatrixX = GetRodriguesRotationMatrix(forward, rotation.X);
            var rotateMatrixY = GetRodriguesRotationMatrix(forward, rotation.Y);
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
            var translateMatrix = new Matrix4x4()
            {
                M11 = 1,
                M22 = 1,
                M33 = 1,
                M14 = translation.X,
                M24 = translation.Y,
                M34 = translation.Z,
                M44 = 1,
            };

            return Matrix4x4.Transpose(translateMatrix);
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
    }
}
