using System;
using Foundation;
using UIKit;
using CoreGraphics;
using OpenTK.Graphics.ES20;
using Pikkart.ArSdk.Recognition;

namespace XamariniOS_LocalPlanarMarker
{
	public class Mesh : NSObject
	{
		float[] mVertices= {};
    	float[] mTexCoords={};
        float[] mNormals={};
    	ushort[] mIndex={};
    	int mIndices_Number = 0;
    	int mVertices_Number = 0;
    	int mTexture_GL_ID = 0;
        int mKeyframeTexture_GL_ID = 0;
    	int mIconBusyTexture_GL_ID = 0;
    	int mIconPlayTexture_GL_ID = 0;
    	int mIconErrorTexture_GL_ID = 0;
    	int mProgram_GL_ID = 0;
    
        int mVideo_Program_GL_ID= 0;
    	int mKeyframe_Program_GL_ID = 0;
        string mMeshPath = "";
    	int mSeekPosition = 0;
    	bool mAutostart = false;
    
    	float keyframeAspectRatio = 1f;
    	float videoAspectRatio = 1f;
    
    	float[] mTexCoordTransformationMatrix= {};
    	float[] videoTextureCoords = {0f,1f,1f, 1f,1f, 0f,0f, 0f};
    	float[] videoTextureCoordsTransformed = {0f, 0f, 1f, 0f, 1f, 1f, 0f, 1f};
    	float[] mVideoTexCoords  = {0f,1f,1f, 1f,1f, 0f,0f, 0f};
        internal static string MESH_VERTEX_SHADER = "\n\n"+"attribute vec4 vertexPosition;\n"+"attribute vec2 vertexTexCoord;\n\n"+"varying vec2 texCoord;\n\n"+"uniform mat4 modelViewProjectionMatrix;\n\n"+"void main() \n{\n"+"   gl_Position = modelViewProjectionMatrix * vertexPosition;\n"+"   texCoord = vertexTexCoord;\n"+"}\n";
        internal static string MESH_FRAGMENT_SHADER = "\n\n"+"precision mediump float;\n\n"+"varying vec2 texCoord;\n\n"+"uniform sampler2D texSampler2D;\n\n"+"void main()\n{\n"+"   gl_FragColor = texture2D(texSampler2D, texCoord);\n"+"   //gl_FragColor = vec4(1.0,0.0,0.0,1.0);\n"+"}\n";
      	bool GenerateMesh()  {
        	mVertices = new float[]{	0f, 0f, 0f,
                     		1f, 0f, 0f,
                     		1f, 1f, 0f,
                     		0f, 1f, 0f 
						};
        	mVertices_Number = 4;
        	mTexCoords = new float[]{0f, 1f, 1f, 1f, 1f, 0f, 0f, 0f};
        	mNormals = new float[]{ 0f, 0f, 1f,
            	         0f, 0f, 1f,
                	     0f, 0f, 1f,
                    	 0f, 0f, 1f};
        
        	mIndex = new ushort[]{0, 1, 2, 2, 3, 0};
        	mIndices_Number = 6;
        
        	return true;
    	}
    
		public Mesh () {}
		
		public bool InitMesh(string meshFile, string textureFile) {
			GenerateMesh();
        
        	CGSize dims = CGSize.Empty;
            mMeshPath = meshFile;
        	mTexture_GL_ID = RenderUtils.LoadTextureFromFileName(textureFile, out dims);
        	mKeyframe_Program_GL_ID = RenderUtils.CreateProgram(MESH_VERTEX_SHADER, MESH_FRAGMENT_SHADER);        
            return true;
		}

        float[] VerticesToFloat(NSArray verticesArray) {
            nuint xIndex = 0, yIndex = 1, zIndex = 2;
            var nbrVertices=verticesArray.Count;
            float [] outputArray=new float[verticesArray.Count];
            nuint i=0;
            NSNumber vertex1, vertex2, vertex3;
            while (i < nbrVertices) {
                vertex1=verticesArray.GetItem<NSNumber>(i+xIndex);
                vertex2=verticesArray.GetItem<NSNumber>(i+yIndex);
                vertex3=verticesArray.GetItem<NSNumber>(i+zIndex);
                outputArray[i+xIndex]=(float)(vertex1.FloatValue*-0.2 + 0.5);
                outputArray[i+yIndex]=(float)(vertex2.FloatValue*-0.2 + 0.5);
                outputArray[i+zIndex]=(float)(vertex3.FloatValue*-0.2);
                i=i+3;
            }
            return outputArray;
        }
        
        float[] ToFloatArray(NSArray verticesArray)
        {
            if (verticesArray== null)
                return null; 
         
            float[] output = new float[verticesArray.Count];
            for (nuint i = 0; i < verticesArray.Count; i++)
                output[i] = verticesArray.GetItem<NSNumber>(i).FloatValue;
         
            return output;
        }
        
        ushort[] ToShortIntArray(NSArray verticesArray)
        {
            if (verticesArray== null)
                return null; 
         
            ushort[] output = new ushort[verticesArray.Count];
            for (nuint i = 0; i < verticesArray.Count; i++)
                output[i] = verticesArray.GetItem<NSNumber>(i).UInt16Value;
         
            return output;
        }

        public bool LoadMesh() {
            NSInputStream inputStream=NSInputStream.FromFile(mMeshPath);
            if (inputStream != null) {
                inputStream.Open();
                NSError error;
                try {
                    var jsonMesh=NSJsonSerialization.Deserialize(inputStream,new NSJsonReadingOptions(), out error);
                    if (error == null) {
                        NSArray values=null;
                        nuint i;
                        NSDictionary dictVertices;
                        NSDictionary dictTriangles;

                        NSArray arrayVertices=(jsonMesh as NSDictionary)["vertices"] as NSArray;
                        NSArray arrayTriangles=(jsonMesh as NSDictionary)["connectivity"] as NSArray;
                        NSString nameVertices, nameTriangles;
                        for (i=0; i < arrayVertices.Count; i++) {
                            dictVertices=arrayVertices.GetItem<NSDictionary>(i);
                            values=dictVertices["values"] as NSArray;
                            nameVertices=dictVertices["name"] as NSString;
                            if (nameVertices == "position_buffer") {
                                mVertices=VerticesToFloat(values);
                            } else if (nameVertices == "normal_buffer") {
                                mNormals=ToFloatArray(values);
                            } else if (nameVertices == "texcoord_buffer") {
                                mTexCoords=ToFloatArray(values);
                            } 
                        }

                        for (i=0; i < arrayTriangles.Count; i++) {
                            dictTriangles=arrayTriangles.GetItem<NSDictionary>(i);
                            nameTriangles=dictTriangles["name"] as NSString;
                            if (nameTriangles == "triangles") {
                                values = dictTriangles["indices"] as NSArray;
                                mIndices_Number=(int)values.Count;
                                mIndex=ToShortIntArray(values);
                            }
                        }
                     }
                } catch (Exception ee) {
                    Console.WriteLine("Errore durante LoadMesh {0}", ee.Message);
                    inputStream.Close();                   
                    return false;
                }
                inputStream.Close();                   
            }
            return true;
        }

        /*
glEnable(GLenum(GL_DEPTH_TEST));
        glDisable(GLenum(GL_CULL_FACE));
        
        glUseProgram(_Program_GL_ID);
        
        RenderUtils.checkGLError()
        
        let vertexHandle = glGetAttribLocation(_Program_GL_ID, "vertexPosition");
        let textureCoordHandle = glGetAttribLocation(_Program_GL_ID, "vertexTexCoord");
        let mvpMatrixHandle = glGetUniformLocation(_Program_GL_ID, "modelViewProjectionMatrix");
        let texSampler2DHandle = glGetUniformLocation(_Program_GL_ID, "texSampler2D");
        
        RenderUtils.checkGLError()
        
        
        glVertexAttribPointer(GLuint(vertexHandle), 3, GLenum(GL_FLOAT), GLboolean(GL_FALSE), 0, _vertices_buffer);
        glVertexAttribPointer(GLuint(textureCoordHandle), 2, GLenum(GL_FLOAT), GLboolean(GL_FALSE), 0, _tex_vertices_buffer);
        
        glEnableVertexAttribArray(GLuint(vertexHandle));
        glEnableVertexAttribArray(GLuint(textureCoordHandle));
        
        RenderUtils.checkGLError()
        
        // activate texture 0, bind it, and pass to shader
        glActiveTexture(GLenum(GL_TEXTURE0));
        glBindTexture(GLenum(GL_TEXTURE_2D), _Texture_GL_ID);
        glUniform1i(texSampler2DHandle, 0);
        RenderUtils.checkGLError()
        
        // pass the model view matrix to the shader
        glUniformMatrix4fv(mvpMatrixHandle, 1, GLboolean(GL_FALSE), modelViewProjection);
        
        RenderUtils.checkGLError()
        
        // finally draw the monkey
        glDrawElements(GLenum(GL_TRIANGLES), GLsizei(_indexes_number), GLenum(GL_UNSIGNED_SHORT), _indexes_buffer);
        
        RenderUtils.checkGLError()
        
        glDisableVertexAttribArray(GLuint(vertexHandle));
        glDisableVertexAttribArray(GLuint(textureCoordHandle));
        
        RenderUtils.checkGLError()
        */   
    	public void DrawMesh(ref float[] mvpMatrix) {
    
	        GL.Enable(EnableCap.DepthTest);
	        GL.Disable(EnableCap.CullFace);
	
	        GL.UseProgram(mKeyframe_Program_GL_ID);
	
	        RenderUtils.CheckGLError();
	
	        var vertexHandle = GL.GetAttribLocation(mKeyframe_Program_GL_ID, "vertexPosition");
	        var textureCoordHandle = GL.GetAttribLocation(mKeyframe_Program_GL_ID, "vertexTexCoord");
	        var mvpMatrixHandle = GL.GetUniformLocation(mKeyframe_Program_GL_ID, "modelViewProjectionMatrix");
	        var texSampler2DHandle = GL.GetUniformLocation(mKeyframe_Program_GL_ID, "texSampler2D");
	
	        RenderUtils.CheckGLError();
	
	        GL.VertexAttribPointer(vertexHandle, 3, VertexAttribPointerType.Float, false, 0, mVertices);
	        GL.VertexAttribPointer(textureCoordHandle, 2,VertexAttribPointerType.Float, false, 0, mTexCoords);
	
	
	        GL.EnableVertexAttribArray(vertexHandle);
	        GL.EnableVertexAttribArray(textureCoordHandle);
	
	        RenderUtils.CheckGLError();
	
	        GL.ActiveTexture(TextureUnit.Texture0);
	        GL.BindTexture(TextureTarget.Texture2D, mTexture_GL_ID);
	        GL.Uniform1(texSampler2DHandle, 0);
	
	        RenderUtils.CheckGLError();
	
	        GL.Ext.PushGroupMarker(0, "Draw Pikkart KeyFrame");
	
	        GL.UniformMatrix4(mvpMatrixHandle, 1, false, mvpMatrix);
	
	        GL.DrawElements(BeginMode.Triangles, mIndices_Number, DrawElementsType.UnsignedShort, mIndex);
	
	        GL.Ext.PopGroupMarker();
	
	        RenderUtils.CheckGLError();
	
	        GL.DisableVertexAttribArray(vertexHandle);
	        GL.DisableVertexAttribArray(textureCoordHandle);
	        
	        RenderUtils.CheckGLError();
	
	    }

	}
}
