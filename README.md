# Sistema de tele operación a distancia de robot NAO

<p align="center">
  <video src="https://github.com/user-attachments/assets/7a76f9d8-64b8-4f23-bf22-9ebda2fc27f7" width="80%" controls></video>
</p>

Este proyecto fue desarrollado y probado utilizando la siguiente configuración:

* **Hardware:** Robot Humanoide NAO v4 (Aldebran Robotics)
* **Sistema Operativo del Robot:** NAOqui v1.14.5
* **Drivers necesarios:** Kinect for Windows SDK v1.8
* **Entorno de Desarrollo:** Choregraphe v1.14.5
* **Lenguaje de Programación:** Python 2.7 (Nativo de las caja de código de Choregraphe) & C# para software de track de esqueleto del usuario.

## Estructura del repositorio

```
├── Software track/                          # Software tracking desarrollado en C#
│   ├── KinectViewer.cs
├── Cajas de código/                         # Código de python para cajas de código en Choregraphe
│   ├── imitacion.py/
│   └── baile.py/
|── Compilación/     # Comando para compilar el archivo C#
|    |-- Comando_compilador
|-- Troubleshooting.md                       # Problemas comunes durante el desarrollo y como solucionarlos.
```


Estudiante: Christopher Jared Villa Alanis

Proyecto desarrollado como parte de un proyecto integral.
