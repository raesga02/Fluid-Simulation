#define FLOAT_MAX 3.402823e+38
#define FLOAT_MIN (- FLOAT_MAX)


// Structs

struct ProjectionLine {
    float start;
    float end;
};

struct ParticleCollider {
    float3 position;
    float radius;
};

struct AABB {
    float3 min;
    float3 max;
};

struct SimulationCollider {
    int vertexStartIdx;
    int numVertices;
    int normalStartIdx;
    int numCollisionNormals;
    int isBounds;
    AABB aabb;
};

struct CollisionDisplacement {
    float magnitude;
    float3 direction;
    float3 displacement;
};


// Buffers
RWStructuredBuffer<SimulationCollider> _CollidersLookup;
RWStructuredBuffer<float3> _CollidersVertices;
RWStructuredBuffer<float3> _CollidersCollisionNormals;


// Vector Math Helpers

float3 ReflectAndDampVelocity(float3 velocity, float3 normal, float collisionDamping) {
    return velocity - (collisionDamping + 1) * dot(velocity, normal) * normal;
}


// Collision Helpers

ProjectionLine ProjectCollider(SimulationCollider collider, float3 normal) {
    ProjectionLine colliderProjection = { FLOAT_MAX, FLOAT_MIN };
    
    for (int i = 0; i < collider.numVertices; i++) {
        float vertexProjection = dot(_CollidersVertices[collider.vertexStartIdx + i], normal);
        colliderProjection.start = min(vertexProjection, colliderProjection.start);
        colliderProjection.end = max(vertexProjection, colliderProjection.end);
    }

    return colliderProjection;
}

ProjectionLine ProjectParticle(ParticleCollider particle, float3 normal) {
    float particleCentreProjection = dot(particle.position, normal);
    ProjectionLine particleProjection =  { particleCentreProjection - particle.radius, particleCentreProjection + particle.radius};
    return particleProjection;
}

bool isOutsideAABB(ParticleCollider particle, AABB aabb) {
    float3 aabbHalfSize = (aabb.max - aabb.min) * 0.5 - particle.radius;
    float3 aabbCentre = aabb.min + aabbHalfSize + particle.radius;
    
    float3 distCentreToParticle = particle.position - aabbCentre;
    float3 quadrant = sign(distCentreToParticle);
    float3 dst = aabbHalfSize - quadrant * distCentreToParticle;

    return all(step(0.0, - dst));
}


// Collision responses

float GetCollisionResponseOverLine(ProjectionLine particleProjection, ProjectionLine colliderProjection) {
    if (particleProjection.start >= colliderProjection.end || particleProjection.end <= colliderProjection.start) {
        return 0.0;
    }

    return colliderProjection.end - particleProjection.start;
}

CollisionDisplacement GetSolidCollisionResponse(ParticleCollider particle, SimulationCollider collider) {
    CollisionDisplacement collisionResponse = { FLOAT_MAX, float3(0.0, 0.0, 0.0), float3(0.0, 0.0, 0.0) };

    // Early discard with the aabb
    if (isOutsideAABB(particle, collider.aabb)) {
        collisionResponse.magnitude = 0.0;
        return collisionResponse;
    }

    for (int i = 0; i < collider.numCollisionNormals; i++) {
        float3 normal = _CollidersCollisionNormals[collider.normalStartIdx + i];
        ProjectionLine particleProjection = ProjectParticle(particle, normal);
        ProjectionLine colliderProjection = ProjectCollider(collider, normal);
        float responseMagnitude = GetCollisionResponseOverLine(particleProjection, colliderProjection);

        // If there is no overlap
        if (responseMagnitude == 0.0) {
            collisionResponse.magnitude = 0.0;
            return collisionResponse;
        }
        
        // If there is a potential overlap
        if (abs(responseMagnitude) < abs(collisionResponse.magnitude)) {
            collisionResponse.magnitude = responseMagnitude;
            collisionResponse.direction = normal;
            collisionResponse.displacement = responseMagnitude * normal;
        }
    }

    return collisionResponse;
}

float3 GetVelocityAdjustment(float3 velocity, float3 gravity) {
    float3 isGravityOnAxis = step(abs(gravity), 0.0);
    float3 isVelocitySmall = abs(velocity) < 0.001;
    return - isGravityOnAxis * isVelocitySmall * 0.1;
}

CollisionDisplacement GetHollowCollisionResponse(ParticleCollider particle, SimulationCollider collider) {
    CollisionDisplacement collisionResponse = { FLOAT_MAX, float3(0.0, 0.0, 0.0), float3(0.0, 0.0, 0.0) };
    float3 aabbHalfSize = (collider.aabb.max - collider.aabb.min) * 0.5 - particle.radius;
    float3 aabbCentre = collider.aabb.min + aabbHalfSize + particle.radius;
    
    float3 distCentreToParticle = particle.position - aabbCentre;
    float3 quadrant = sign(distCentreToParticle);
    float3 dst = aabbHalfSize - quadrant * distCentreToParticle;

    float3 needsToBounce = step(0.0, - dst);
    float3 displacement = (quadrant * aabbHalfSize + aabbCentre - particle.position) * needsToBounce;

    collisionResponse.magnitude = length(displacement);
    collisionResponse.direction = - quadrant * needsToBounce;
    collisionResponse.displacement = displacement;

    return collisionResponse;
}