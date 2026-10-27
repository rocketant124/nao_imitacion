# Este algoritmo solo funciona para el robot NAO virtual

# se importan las librerias necesarias
import json
import socket
import threading

# Este script permite controlar los movimientos de la cabeza y brazos del robot NAO a través de un archivo JSON externo. 
# El robot leerá los ángulos deseados desde el archivo y ajustará sus articulaciones en consecuencia, 
# mientras mantiene las piernas libres para otros movimientos.
class MyClass(GeneratedClass):
    def __init__(self):
        GeneratedClass.__init__(self)
        self.running = False
        self.sock = None
        self.thread = None
        self.motion = None

    def onLoad(self):
        pass

    def onUnload(self):
        self._stop()

    def onInput_onStart(self):
        try:
            self.motion = ALProxy("ALMotion")
            
            # Desactivar FallManager para evitar pánico de caídas en cuclillas/sentado
            try:
                self.motion.setFallManagerEnabled(False)
            except:
                pass

            # Activar rigidez en el tren superior, cabeza, brazos y se desactivan las piernas
            self.motion.setStiffnesses("Legs", 0.0)
            self.motion.setStiffnesses("Head", 1.0)
            self.motion.setStiffnesses("LArm", 1.0)
            self.motion.setStiffnesses("RArm", 1.0)
        except Exception as e:
            self.logger.error("Error iniciando ALMotion: " + str(e))
            return

        try:
            # En el robot real (Linux) el socket se crea sin errores
            self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
            self.sock.bind(("0.0.0.0", 5005))
            self.sock.settimeout(0.5)
            self.running = True
            self.logger.info(">>> RECEPTOR UDP LISTO EN PUERTO 5005 <<<")
        except Exception as e:
            self.logger.error("Error iniciando socket UDP: " + str(e))
            return

        def loop_recepcion():
            names = [
                "HeadYaw", "HeadPitch",
                "LShoulderPitch", "LShoulderRoll", "LElbowYaw", "LElbowRoll",
                "RShoulderPitch", "RShoulderRoll", "RElbowYaw", "RElbowRoll"
            ]
            while self.running:
                try:
                    data, addr = self.sock.recvfrom(1024)
                    if data:
                        payload = json.loads(data.decode("utf-8"))

# Extrae los angulos de las articulaciones atraves del archivo JSON, si los valores estan vacios, se asigna un valor por defecto   
                        angles = [
                            float(payload.get("HeadYaw", 0.0)),
                            float(payload.get("HeadPitch", 0.0)),
                            float(payload.get("LShoulderPitch", 0.0)),
                            float(payload.get("LShoulderRoll", 0.0)),
                            float(payload.get("LElbowYaw", -1.2)),
                            float(payload.get("LElbowRoll", -0.1)),
                            float(payload.get("RShoulderPitch", 0.0)),
                            float(payload.get("RShoulderRoll", 0.0)),
                            float(payload.get("RElbowYaw", 1.2)),
                            float(payload.get("RElbowRoll", 0.1))
                        ]
                        self.motion.setAngles(names, angles, 0.35)
                except socket.timeout:
                    continue
                except Exception as err:
                    pass

        self.thread = threading.Thread(target=loop_recepcion)
        self.thread.daemon = True
        self.thread.start()

    def _stop(self):
        self.running = False
        if self.thread:
            self.thread.join(timeout=1.0)
        if self.sock:
            self.sock.close()
        try:
            if self.motion:
                self.motion.setStiffnesses("Head", 0.0)
                self.motion.setStiffnesses("LArm", 0.0)
                self.motion.setStiffnesses("RArm", 0.0)
        except:
            pass

    def onInput_onStop(self):
        self._stop()
        self.onStopped()
