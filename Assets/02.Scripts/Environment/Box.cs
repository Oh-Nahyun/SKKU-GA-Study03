using UnityEngine;

// TODO : 공격 받으면 산산조각 난듯 부서진다.
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Box : MonoBehaviour, IDamageable
{
    [Header("파편")]
    [Min(1)][SerializeField] private int _fragmentsPerAxis = 3; // 한 변을 나눌 조각 수 (3이면 27조각)

    [SerializeField] private float _fragmentLifetime = 3f; // 파편이 남아 있는 시간(초)

    [Header("폭발력")]
    [SerializeField] private float _explosionForce = 300f;

    [SerializeField] private float _explosionRadius = 3f;
    [SerializeField] private float _explosionUpwardsModifier = 0.5f;

    private bool _isBroken = false;

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (_isBroken) return;

        _isBroken = true;
        Break(damageInfo.HitPoint);
    }

    private void Break(Vector3 hitPoint)
    {
        Mesh mesh = GetComponent<MeshFilter>().sharedMesh;
        Material material = GetComponent<MeshRenderer>().sharedMaterial;

        // 메시 로컬 기준 조각 크기와, 월드 기준 조각 크기
        Vector3 localFragmentSize = mesh.bounds.size / _fragmentsPerAxis;
        Vector3 worldFragmentSize = Vector3.Scale(mesh.bounds.size, transform.lossyScale) / _fragmentsPerAxis;

        for (int x = 0; x < _fragmentsPerAxis; x++)
        {
            for (int y = 0; y < _fragmentsPerAxis; y++)
            {
                for (int z = 0; z < _fragmentsPerAxis; z++)
                {
                    // 조각의 중심 위치 (메시 로컬 좌표 -> 월드 좌표)
                    Vector3 gridOffset = new Vector3(x + 0.5f, y + 0.5f, z + 0.5f);
                    Vector3 localCenter = mesh.bounds.min + Vector3.Scale(gridOffset, localFragmentSize);
                    Vector3 worldCenter = transform.TransformPoint(localCenter);

                    CreateFragment(worldCenter, worldFragmentSize, material, hitPoint);
                }
            }
        }

        Destroy(gameObject);
    }

    private void CreateFragment(Vector3 position, Vector3 size, Material material, Vector3 hitPoint)
    {
        GameObject fragment = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fragment.transform.SetPositionAndRotation(position, transform.rotation);
        fragment.transform.localScale = size;
        fragment.GetComponent<MeshRenderer>().sharedMaterial = material;

        // 맞은 자리에서 바깥쪽으로 튀어 나가게 함
        Rigidbody rigidbody = fragment.AddComponent<Rigidbody>();
        rigidbody.AddExplosionForce(_explosionForce, hitPoint, _explosionRadius, _explosionUpwardsModifier);

        Destroy(fragment, _fragmentLifetime);
    }
}