using System;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using Microsoft.Kinect;

namespace KinectVisualizer
{
    public class MainForm : Form
    {
        private KinectSensor sensor;
        private Skeleton[] skeletons;
        private string rutaArchivo = @"C:\Temp\nao_angles.json";
        private string estadoGuardado = "Esperando esqueleto...";

        private UdpClient clienteUdp = new UdpClient();
        private string ipNao = "169.254.183.96"; // IP real de tu NAO físico
        private int puertoNao = 5005;
        private double[] angulosAnteriores = new double[10];
        private double alphaSuavizado = 0.35; // Suavizado EMA

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        public MainForm()
        {
            this.Text = "Kinect v1 - Monitor y Puente NAO (10 DoF)";
            this.Size = new Size(640, 500);
            this.DoubleBuffered = true;
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.FormClosing += MainForm_FormClosing;

            string carpeta = Path.GetDirectoryName(rutaArchivo);
            if (!Directory.Exists(carpeta))
            {
                Directory.CreateDirectory(carpeta);
            }

            InicializarKinect();
        }

        private void InicializarKinect()
        {
            if (KinectSensor.KinectSensors.Count == 0)
            {
                MessageBox.Show("No se detectó ningún sensor Kinect.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            sensor = KinectSensor.KinectSensors[0];
            TransformSmoothParameters smoothing = new TransformSmoothParameters
            {
                Smoothing = 0.5f,
                Correction = 0.5f,
                Prediction = 0.5f,
                JitterRadius = 0.05f,
                MaxDeviationRadius = 0.04f
            };

            sensor.SkeletonStream.Enable(smoothing);
            sensor.SkeletonFrameReady += Sensor_SkeletonFrameReady;

            try
            {
                sensor.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al iniciar Kinect: " + ex.Message);
            }
        }

        private void Sensor_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame frame = e.OpenSkeletonFrame())
            {
                if (frame == null) return;

                skeletons = new Skeleton[frame.SkeletonArrayLength];
                frame.CopySkeletonDataTo(skeletons);

                bool esqueletoDetectado = false;
                foreach (Skeleton skel in skeletons)
                {
                    if (skel.TrackingState == SkeletonTrackingState.Tracked)
                    {
                        GuardarAngulosNAO(skel);
                        esqueletoDetectado = true;
                        break;
                    }
                }

                if (!esqueletoDetectado)
                {
                    estadoGuardado = "Buscando esqueleto (Ponte frente a la cámara)...";
                }

                this.Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            using (Font font = new Font("Arial", 12, FontStyle.Bold))
            using (Brush brush = new SolidBrush(Color.Yellow))
            {
                g.DrawString("Estado: " + estadoGuardado, font, brush, 10, 10);
            }

            if (skeletons == null) return;

            foreach (Skeleton skel in skeletons)
            {
                if (skel.TrackingState == SkeletonTrackingState.Tracked)
                {
                    DibujarHueso(g, skel, JointType.Head, JointType.ShoulderCenter);
                    DibujarHueso(g, skel, JointType.ShoulderCenter, JointType.Spine);
                    DibujarHueso(g, skel, JointType.Spine, JointType.HipCenter);

                    // Brazo Izquierdo
                    DibujarHueso(g, skel, JointType.ShoulderCenter, JointType.ShoulderLeft);
                    DibujarHueso(g, skel, JointType.ShoulderLeft, JointType.ElbowLeft);
                    DibujarHueso(g, skel, JointType.ElbowLeft, JointType.WristLeft);

                    // Brazo Derecho
                    DibujarHueso(g, skel, JointType.ShoulderCenter, JointType.ShoulderRight);
                    DibujarHueso(g, skel, JointType.ShoulderRight, JointType.ElbowRight);
                    DibujarHueso(g, skel, JointType.ElbowRight, JointType.WristRight);
                }
            }
        }

        private void DibujarHueso(Graphics g, Skeleton skel, JointType j1, JointType j2)
        {
            Joint joint1 = skel.Joints[j1];
            Joint joint2 = skel.Joints[j2];

            if (joint1.TrackingState == JointTrackingState.NotTracked || joint2.TrackingState == JointTrackingState.NotTracked)
                return;

            Point p1 = MapearCoordenadas(joint1.Position);
            Point p2 = MapearCoordenadas(joint2.Position);

            using (Pen pen = new Pen(Color.LimeGreen, 4))
            {
                g.DrawLine(pen, p1, p2);
            }

            using (Brush brush = new SolidBrush(Color.White))
            {
                g.FillEllipse(brush, p1.X - 5, p1.Y - 5, 10, 10);
                g.FillEllipse(brush, p2.X - 5, p2.Y - 5, 10, 10);
            }
        }

        private Point MapearCoordenadas(SkeletonPoint pos)
        {
            float x = (pos.X + 1.0f) / 2.0f * (this.ClientSize.Width - 40) + 20;
            float y = (1.0f - pos.Y) / 2.0f * (this.ClientSize.Height - 40) + 20;
            return new Point((int)x, (int)y);
        }

        private void GuardarAngulosNAO(Skeleton skel)
        {
            Joint shC = skel.Joints[JointType.ShoulderCenter];
            Joint hd = skel.Joints[JointType.Head];
            Joint shL = skel.Joints[JointType.ShoulderLeft];
            Joint elL = skel.Joints[JointType.ElbowLeft];
            Joint wrL = skel.Joints[JointType.WristLeft];
            Joint shR = skel.Joints[JointType.ShoulderRight];
            Joint elR = skel.Joints[JointType.ElbowRight];
            Joint wrR = skel.Joints[JointType.WristRight];

            if (shL.TrackingState == JointTrackingState.NotTracked || elL.TrackingState == JointTrackingState.NotTracked ||
                shR.TrackingState == JointTrackingState.NotTracked || elR.TrackingState == JointTrackingState.NotTracked)
            {
                estadoGuardado = "Brazos no detectados de forma clara";
                return;
            }

            // 1. CABEZA
            float dxH = hd.Position.X - shC.Position.X;
            float dyH = hd.Position.Y - shC.Position.Y;
            double headYaw = (Math.Abs(dxH) > 0.02f) ? dxH * 1.8 : 0.0;
            double headPitch = -(dyH - 0.28f) * 1.5;

            // 2. BRAZO IZQUIERDO
            float vxL = elL.Position.X - shL.Position.X;
            float vyL = elL.Position.Y - shL.Position.Y;
            float vzL = elL.Position.Z - shL.Position.Z;
            double magL = Math.Max(0.05, Math.Sqrt(vxL * vxL + vyL * vyL + vzL * vzL));

            double lPitchRaw = Math.Atan2(-vyL, Math.Max(0.02, Math.Abs(vzL)));
            double lRollRaw = Math.Asin(Math.Max(-1.0, Math.Min(1.0, -vxL / magL)));
            double lElbowRaw = (wrL.TrackingState != JointTrackingState.NotTracked) 
                ? -CalcularAnguloCodo(shL.Position, elL.Position, wrL.Position) : -0.2;

            // 3. BRAZO DERECHO
            float vxR = elR.Position.X - shR.Position.X;
            float vyR = elR.Position.Y - shR.Position.Y;
            float vzR = elR.Position.Z - shR.Position.Z;
            double magR = Math.Max(0.05, Math.Sqrt(vxR * vxR + vyR * vyR + vzR * vzR));

            double rPitchRaw = Math.Atan2(-vyR, Math.Max(0.02, Math.Abs(vzR)));
            double rRollRaw = -Math.Asin(Math.Max(-1.0, Math.Min(1.0, vxR / magR)));
            double rElbowRaw = (wrR.TrackingState != JointTrackingState.NotTracked) 
                ? CalcularAnguloCodo(shR.Position, elR.Position, wrR.Position) : 0.2;

            // 4. LÍMITES MECÁNICOS DE SEGURIDAD
            double[] actuales = new double[10] {
                Math.Max(-1.5, Math.Min(1.5, headYaw)),      // HeadYaw
                Math.Max(-0.5, Math.Min(0.4, headPitch)),    // HeadPitch
                Math.Max(-2.0, Math.Min(2.0, lPitchRaw)),    // LShoulderPitch
                Math.Max(-0.31, Math.Min(1.32, lRollRaw)),   // LShoulderRoll
                -1.2,                                        // LElbowYaw
                Math.Max(-1.54, Math.Min(-0.03, lElbowRaw)), // LElbowRoll
                Math.Max(-2.0, Math.Min(2.0, rPitchRaw)),    // RShoulderPitch
                Math.Max(-1.32, Math.Min(0.31, rRollRaw)),   // RShoulderRoll
                1.2,                                         // RElbowYaw
                Math.Max(0.03, Math.Min(1.54, rElbowRaw))    // RElbowRoll
            };

            // 5. SUAVIZADO EMA
            for (int i = 0; i < 10; i++)
            {
                if (double.IsNaN(actuales[i]) || double.IsInfinity(actuales[i]))
                {
                    actuales[i] = angulosAnteriores[i];
                }
                angulosAnteriores[i] = (alphaSuavizado * actuales[i]) + ((1.0 - alphaSuavizado) * angulosAnteriores[i]);
            }

            // 6. FORMATO Y ENVÍO UDP
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            string json = "{\"HeadYaw\":" + angulosAnteriores[0].ToString("F3", ci) +
                          ",\"HeadPitch\":" + angulosAnteriores[1].ToString("F3", ci) +
                          ",\"LShoulderPitch\":" + angulosAnteriores[2].ToString("F3", ci) +
                          ",\"LShoulderRoll\":" + angulosAnteriores[3].ToString("F3", ci) +
                          ",\"LElbowYaw\":" + angulosAnteriores[4].ToString("F3", ci) +
                          ",\"LElbowRoll\":" + angulosAnteriores[5].ToString("F3", ci) +
                          ",\"RShoulderPitch\":" + angulosAnteriores[6].ToString("F3", ci) +
                          ",\"RShoulderRoll\":" + angulosAnteriores[7].ToString("F3", ci) +
                          ",\"RElbowYaw\":" + angulosAnteriores[8].ToString("F3", ci) +
                          ",\"RElbowRoll\":" + angulosAnteriores[9].ToString("F3", ci) + "}";

            try 
            { 
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                clienteUdp.Send(bytes, bytes.Length, ipNao, puertoNao);
                estadoGuardado = "Enviando UDP OK a " + ipNao;
            } 
            catch (Exception ex) 
            { 
                estadoGuardado = "Error red UDP: " + ex.Message; 
            }
        }

        private double CalcularAnguloCodo(SkeletonPoint sh, SkeletonPoint el, SkeletonPoint wr)
        {
            float v1x = sh.X - el.X; float v1y = sh.Y - el.Y; float v1z = sh.Z - el.Z;
            float v2x = wr.X - el.X; float v2y = wr.Y - el.Y; float v2z = wr.Z - el.Z;

            double dot = (v1x * v2x) + (v1y * v2y) + (v1z * v2z);
            double mag1 = Math.Sqrt(v1x * v1x + v1y * v1y + v1z * v1z);
            double mag2 = Math.Sqrt(v2x * v2x + v2y * v2y + v2z * v2z);

            if (mag1 * mag2 < 0.001) return 0.1;

            double cosAngle = Math.Max(-1.0, Math.Min(1.0, dot / (mag1 * mag2)));
            return (Math.PI - Math.Acos(cosAngle));
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (sensor != null && sensor.IsRunning)
            {
                sensor.Stop();
                sensor = null;
            }
            clienteUdp.Close();
        }
    }
}
