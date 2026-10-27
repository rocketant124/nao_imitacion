import time

class MyClass(GeneratedClass):
    def __init__(self):
        GeneratedClass.__init__(self)

    def onLoad(self):
        pass

    def onUnload(self):
        pass

    def onInput_onStart(self):
        try:
            motion = ALProxy("ALMotion")
            
            # 1. Asegurar rigidez en tren superior y liberar piernas por seguridad
            motion.setStiffnesses("Legs", 0.0)
            motion.setStiffnesses("LArm", 1.0)
            motion.setStiffnesses("RArm", 1.0)
            motion.setStiffnesses("Head", 1.0)

            # Lista de articulaciones
            joints = [
                "LShoulderPitch", "LShoulderRoll", "LElbowYaw", "LElbowRoll",
                "RShoulderPitch", "RShoulderRoll", "RElbowYaw", "RElbowRoll",
                "HeadYaw", "HeadPitch"
            ]

            speed = 0.35 # Velocidad alegre pero segura (35% de max)

            self.logger.info("¡Iniciando secuencia de baile!")

            # --- PASO 1: Saludo y Cabeceo inicial ---
            # Levanta brazo derecho y mueve cabeza a la izquierda
            pose1 = [1.2, 0.1, 0.0, -0.2,   -0.5, -0.3, 0.8, 0.8,   -0.4, -0.1]
            motion.setAngles(joints, pose1, speed)
            time.sleep(0.8)

            # Mueve cabeza a la derecha
            motion.setAngles(["HeadYaw"], [0.4], speed)
            time.sleep(0.8)

            # --- PASO 2: El ritmo Disco / Alternado ---
            # Brazo izquierdo arriba, brazo derecho abajo
            pose2 = [-0.6, 0.3, -1.0, -0.9,   1.2, -0.1, 0.5, 0.3,   0.0, 0.0]
            # Brazo izquierdo abajo, brazo derecho arriba
            pose3 = [1.2, 0.1, -0.5, -0.3,   -0.6, -0.3, 1.0, 0.9,   0.0, 0.0]

            # Repetimos la secuencia de ritmo 3 veces
            for i in range(3):
                motion.setAngles(joints, pose2, 0.45) # Algo mas rápido para el ritmo
                time.sleep(0.5)
                motion.setAngles(joints, pose3, 0.45)
                time.sleep(0.5)

            # --- PASO 3: La Ola / Celebración ---
            # Brazos totalmente abiertos en V
            pose_v = [-0.8, 0.5, -1.2, -0.1,   -0.8, -0.5, 1.2, 0.1,   0.0, -0.2]
            motion.setAngles(joints, pose_v, 0.4)
            time.sleep(1.0)

            # Doblar codos hacia adentro (estilo robotico)
            pose_codos = [-0.8, 0.2, -1.5, -1.5,   -0.8, -0.2, 1.5, 1.5,   0.0, 0.1]
            motion.setAngles(joints, pose_codos, 0.5)
            time.sleep(0.8)

            # Regresar a V
            motion.setAngles(joints, pose_v, 0.5)
            time.sleep(0.8)

            # --- FINAL: Posición neutra/relajada ---
            pose_neutra = [1.4, 0.1, 0.0, -0.1,   1.4, -0.1, 0.0, 0.1,   0.0, 0.0]
            motion.setAngles(joints, pose_neutra, 0.2)
            time.sleep(1.5)

            self.logger.info("¡Baile finalizado!")
            self.onStopped()

        except Exception as e:
            self.logger.error("Error durante el baile: " + str(e))

    def onInput_onStop(self):
        try:
            motion = ALProxy("ALMotion")
            # Relajar los motores al terminar o cancelar
            motion.setStiffnesses("LArm", 0.0)
            motion.setStiffnesses("RArm", 0.0)
            motion.setStiffnesses("Head", 0.0)
        except:
            pass
        self.onStopped()