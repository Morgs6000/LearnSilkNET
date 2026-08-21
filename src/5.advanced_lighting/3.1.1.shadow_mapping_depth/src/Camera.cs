using System.Numerics;

namespace LearnSilkNET.src;

// Define várias opções possíveis para o movimento da câmera. Utilizado como uma abstração para evitar a dependência de métodos de entrada específicos do sistema de janelas.
public enum Camera_Movement
{
    FORWARD,
    BACKWARD,
    LEFT,
    RIGHT
}

// Uma classe de câmera abstrata que processa a entrada e calcula os ângulos de Euler, vetores e matrizes correspondentes para uso no OpenGL
public class Camera
{
    // Valores padrão da câmera
    public const float YAW         = -90.0f;
    public const float PITCH       =  0.0f;
    public const float SPEED       =  2.5f;
    public const float SENSITIVITY =  0.1f;
    public const float ZOOM        =  45.0f;

    // Atributos da câmera
    public Vector3 Position;
    public Vector3 Front;
    public Vector3 Up;
    public Vector3 Right;
    public Vector3 WorldUp;

    // Ângulos de Euler
    public float Yaw;
    public float Pitch;

    // opções de câmera
    public float MovementSpeed;
    public float MouseSensitivity;
    public float Zoom;

    // construtor com vetores
    public Camera(Vector3? position = null, Vector3? up = null, float? yaw = null, float? pitch = null)
    {
        Front = new Vector3(0.0f, 0.0f, -1.0f);
        MovementSpeed = SPEED;
        MouseSensitivity = SENSITIVITY;
        Zoom = ZOOM;

        Position = position ?? new Vector3(0.0f, 0.0f, 0.0f);
        WorldUp = up ?? new Vector3(0.0f, 1.0f, 0.0f);
        Yaw = yaw ?? YAW;
        Pitch = pitch ?? PITCH;

        UpdateCameraVectors();
    }

    // construtor com valores escalares
    public Camera(float posX, float posY, float posZ, float upX, float upY, float upZ, float yaw, float pitch)
    {
        Front = new Vector3(0.0f, 0.0f, -1.0f);
        MovementSpeed = SPEED;
        MouseSensitivity = SENSITIVITY;
        Zoom = ZOOM;

        Position = new Vector3(posX, posY, posZ);
        WorldUp = new Vector3(upX, upY, upZ);
        Yaw = yaw;
        Pitch = pitch;

        UpdateCameraVectors();
    }

    // retorna a matriz de visualização calculada usando ângulos de Euler e a matriz LookAt
    public Matrix4x4 GetViewMatrix()
    {
        return Matrix4x4.CreateLookAt(
            cameraPosition: Position,
            cameraTarget:   Position + Front,
            cameraUpVector: Up
        );
    }

    // processa a entrada recebida de qualquer sistema de entrada do tipo teclado. Aceita um parâmetro de entrada na forma de um ENUM definido pela câmera (para abstraí-lo de sistemas de janelas)
    public void ProcessKeyboard(Camera_Movement direction, float deltaTime)
    {
        float velocity = MovementSpeed * deltaTime;

        if (direction == Camera_Movement.FORWARD)
        {
            Position += velocity * Front;
        }
        if (direction == Camera_Movement.BACKWARD)
        {
            Position -= velocity * Front;
        }
        if (direction == Camera_Movement.LEFT)
        {
            Position -= velocity * Right;
        }
        if (direction == Camera_Movement.RIGHT)
        {
            Position += velocity * Right;
        }
    }

    // processa a entrada recebida de um sistema de entrada de mouse. Espera o valor de deslocamento nas direções x e y.
    public void ProcessMouseMovement(float xoffset, float yoffset, bool constrainPitch = true)
    {
        xoffset *= MouseSensitivity;
        yoffset *= MouseSensitivity;

        Yaw   += xoffset;
        Pitch += yoffset;

        // certifique-se de que a tela não seja invertida quando o pitch estiver fora dos limites
        if (constrainPitch)
        {
            if (Pitch > 89.0f)
            {
                Pitch = 89.0f;
            }
            if (Pitch < -89.0f)
            {
                Pitch = -89.0f;
            }
        }

        // atualiza os vetores Front, Right e Up usando os ângulos de Euler atualizados
        UpdateCameraVectors();
    }

    // processa a entrada recebida de um evento de roda de rolagem do mouse. Requer entrada apenas no eixo vertical da roda.
    public void ProcessMouseScroll(float yoffset)
    {
        Zoom -= yoffset;

        if (Zoom < 1.0f)
        {
            Zoom = 1.0f;
        }
        if (Zoom > 45.0f)
        {
            Zoom = 45.0f;
        }
    }

    // calcula o vetor frontal a partir dos ângulos de Euler (atualizados) da câmera
    private void UpdateCameraVectors()
    {
        // calcula o novo vetor Front
        Vector3 front;
        front.X = MathF.Cos(MathHelper.DegreesToRadians(Pitch)) * MathF.Cos(MathHelper.DegreesToRadians(Yaw));
        front.Y = MathF.Sin(MathHelper.DegreesToRadians(Pitch));
        front.Z = MathF.Cos(MathHelper.DegreesToRadians(Pitch)) * MathF.Sin(MathHelper.DegreesToRadians(Yaw));
        Front = Vector3.Normalize(front);

        // também recalcule os vetores Direita e Cima
        Right = Vector3.Normalize(Vector3.Cross(Front, WorldUp)); // Normaliza os vetores, pois o comprimento deles se aproxima de zero quanto mais você olha para cima ou para baixo, o que resulta em um movimento mais lento.
        Up = Vector3.Normalize(Vector3.Cross(Right, Front));
    }
}
