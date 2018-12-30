/*
Bullet Continuous Collision Detection and Physics Library
Copyright (c) 2015 Google Inc. http://bulletphysics.org

This software is provided 'as-is', without any express or implied warranty.
In no event will the authors be held liable for any damages arising from the use of this software.
Permission is granted to anyone to use this software for any purpose,
including commercial applications, and to alter it and redistribute it freely,
subject to the following restrictions:

1. The origin of this software must not be misrepresented; you must not claim that you wrote the original software. If you use this software in a product, an acknowledgment in the product documentation would be appreciated but is not required.
2. Altered source versions must be plainly marked as such, and must not be misrepresented as being the original software.
3. This notice may not be removed or altered from any source distribution.
*/

using DemoFramework;
using BulletSharp;
using BulletSharp.Math;
using BulletSharp.InverseDynamics;
using System;
using System.Globalization;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Collections.Generic;

namespace InverseDynamicsExample
{

    public enum btInverseDynamicsExampleOptions
    {
        BT_ID_LOAD_URDF = 0,
        BT_ID_PROGRAMMATICALLY = 1
    }

    public class InverseDynamicsExampleSimulation : Demo
    {
        btInverseDynamicsExampleOptions m_option = btInverseDynamicsExampleOptions.BT_ID_PROGRAMMATICALLY;
        MultiBody m_multiBody;
        MultiBodyTree m_inverseModel;

        // the UI interface makes it easier to use static variables & free functions
        // as parameters and callbacks
        static float kp = 10 * 10f;
        static float kd = 2 * 10f;
        static bool useInverseModel = true;
        float[] qd;
        string[] qd_name;
        string[] q_name;
        public float radius = 1f;

        //------------- Start CommonMultiBodyBase


        //keep the collision shapes, for deletion/cleanup
        protected OverlappingPairCache m_pairCache;
        protected MultiBodyConstraintSolver m_solver;
        protected MultiBodyDynamicsWorld m_dynamicsWorld;


        BulletSharp.Math.Vector3 m_oldPickingPos;
        BulletSharp.Math.Vector3 m_hitPos;
        float m_oldPickingDist;
        bool m_prevCanSleep;

        public virtual void createEmptyDynamicsWorld()
        {
            ///collision configuration contains default setup for memory, collision setup
            CollisionConf = new DefaultCollisionConfiguration();

            ///use the default collision dispatcher. For parallel processing you can use a diffent dispatcher (see Extras/BulletMultiThreaded)
            Dispatcher = new CollisionDispatcher(CollisionConf);

            m_pairCache = new HashedOverlappingPairCache();

            Broadphase = new DbvtBroadphase(m_pairCache);

            m_solver = new MultiBodyConstraintSolver();

            World = m_dynamicsWorld = new MultiBodyDynamicsWorld(Dispatcher, Broadphase, m_solver, CollisionConf);

            m_dynamicsWorld.Gravity = (new BulletSharp.Math.Vector3(0, -10, 0));
        }


        public override void ExitPhysics()
        {
            if (m_inverseModel != null)
            {
                Debug.Log("Dispose inverse model " + m_inverseModel.NumBodies);
                m_inverseModel.Dispose();
            }

            Debug.Log("InverseDynamicsExitPhysics");
            //cleanup in the reverse order of creation/initialization

            //remove the rigidbodies from the dynamics world and delete them

            if (m_dynamicsWorld == null)
            {

                int i;
                for (i = m_dynamicsWorld.NumConstraints - 1; i >= 0; i--)
                {
                    TypedConstraint tc = m_dynamicsWorld.GetConstraint(i);
                    m_dynamicsWorld.RemoveConstraint(tc);
                    tc.Dispose();
                }

                for (i = m_dynamicsWorld.NumMultiBodyConstraints - 1; i >= 0; i--)
                {
                    MultiBodyConstraint mbc = m_dynamicsWorld.GetMultiBodyConstraint(i);
                    m_dynamicsWorld.RemoveMultiBodyConstraint(mbc);
                    mbc.Dispose();
                }

                for (i = m_dynamicsWorld.NumMultibodies - 1; i >= 0; i--)
                {
                    MultiBody mb = m_dynamicsWorld.GetMultiBody(i);
                    m_dynamicsWorld.RemoveMultiBody(mb);
                    mb.Dispose();
                }
                for (i = m_dynamicsWorld.NumCollisionObjects - 1; i >= 0; i--)
                {
                    CollisionObject obj = m_dynamicsWorld.CollisionObjectArray[i];
                    RigidBody body = RigidBody.Upcast(obj);
                    if (body != null && body.MotionState != null)
                    {
                        body.MotionState.Dispose();
                    }
                    m_dynamicsWorld.RemoveCollisionObject(obj);
                    obj.Dispose();
                }
            }

            if (m_multiBody != null) m_multiBody.Dispose();

            //delete collision shapes
            for (int j = 0; j < CollisionShapes.Count; j++)
            {
                CollisionShape shape = CollisionShapes[j];
                shape.Dispose();
            }
            CollisionShapes.Clear();

            m_dynamicsWorld.Dispose();
            m_dynamicsWorld = null;

            m_solver.Dispose();
            m_solver = null;

            Broadphase.Dispose();
            Broadphase = null;

            Dispatcher.Dispose();
            Dispatcher = null;

            m_pairCache.Dispose();
            m_pairCache = null;

            CollisionConf.Dispose();
            CollisionConf = null;
            Debug.Log("After dispose B");
        }

        public virtual void physicsDebugDraw(DebugDrawModes debugDrawFlags)
        {
            if (m_dynamicsWorld != null)
            {
                if (m_dynamicsWorld.DebugDrawer != null)
                {
                    m_dynamicsWorld.DebugDrawer.DebugMode = (debugDrawFlags);
                }
                m_dynamicsWorld.DebugDrawWorld();
            }

        }

        protected override void OnInitialize()
        {
            //Freelook.SetEyeTarget(eye, target);

            Graphics.SetFormText("BulletSharp - Inverse Dynamics");
        }

        protected override void OnInitializePhysics()
        {
            Debug.Log("Begin onInitialize physics");
            //roboticists like Z up
            int upAxis = 2;
            //m_guiHelper.setUpAxis(upAxis);

            createEmptyDynamicsWorld();
            BulletSharp.Math.Vector3 gravity = new BulletSharp.Math.Vector3(0, 0, 0);
            // gravity[upAxis]=-9.8;
            m_dynamicsWorld.Gravity = (gravity);

            if (m_option == btInverseDynamicsExampleOptions.BT_ID_PROGRAMMATICALLY)
            {
            }

            switch (m_option)
            {
                case btInverseDynamicsExampleOptions.BT_ID_LOAD_URDF:
                    {

                        Debug.LogError("Not implemented");
                        break;
                    }
                case btInverseDynamicsExampleOptions.BT_ID_PROGRAMMATICALLY:
                    {
                        Matrix baseWorldTrans = new Matrix();
                        baseWorldTrans.ScaleVector = BulletSharp.Math.Vector3.One;
                        m_multiBody = createInvertedPendulumMultiBody(radius, m_dynamicsWorld, baseWorldTrans, false);
                        break;
                    }
                default:
                    {
                        Debug.LogError("Unknown option in initPhysics");
                        Debug.Assert(false);
                        break;
                    }
            };


            if (true)//(m_multiBody != null)
            {
                // construct inverse model
                MultiBodyTreeCreator id_creator = new MultiBodyTreeCreator();
                if (-1 == id_creator.CreateFromMultiBody(m_multiBody))
                {
                    Debug.LogError("error creating tree\n");
                }
                else
                {
                    m_inverseModel = id_creator.CreateMultiBodyTree();
                }
                // add joint target controls
                qd = new float[m_multiBody.NumDofs];

                qd_name = new string[m_multiBody.NumDofs];
                q_name = new string[m_multiBody.NumDofs];
                Debug.Log("Created inverse model");
            }

            Debug.Log("End onInitialize physics");
        }

        public override void OnUpdate()
        {
            if (true) //(m_multiBody != null)
            {
                int baseDofs = m_multiBody.HasFixedBase ? 0 : 6;
                int num_dofs = m_multiBody.NumDofs;
                float[] nu = new float[num_dofs + baseDofs];
                float[] qdot = new float[num_dofs + baseDofs];
                float[] q = new float[num_dofs + baseDofs];
                float[] joint_force = new float[num_dofs + baseDofs];
                float[] pd_control = new float[num_dofs + baseDofs];

                // compute joint forces from one of two control laws:
                // 1) "computed torque" control, which gives perfect, decoupled,
                //    linear second order error dynamics per dof in case of a
                //    perfect model and (and negligible time discretization effects)
                // 2) decoupled PD control per joint, without a model
                for (int dof = 0; dof < num_dofs; dof++)
                {
                    q[dof] = m_multiBody.GetJointPos(dof);
                    qdot[dof] = m_multiBody.GetJointVel(dof);

                    float qd_dot = 0;
                    float qd_ddot = 0;
                    //if (m_timeSeriesCanvas)
                    //    m_timeSeriesCanvas.insertDataAtCurrentTime(q[dof], dof, true);

                    // pd_control is either desired joint torque for pd control,
                    // or the feedback contribution to nu
                    pd_control[dof] = kd * (qd_dot - qdot[dof]) + kp * (qd[dof] - q[dof]);
                    // nu is the desired joint acceleration for computed torque control
                    nu[dof] = qd_ddot + pd_control[dof];

                }
                if (true)
                {
                    // calculate joint forces corresponding to desired accelerations nu
                    if (m_multiBody.HasFixedBase)
                    {

                        Debug.Log("Adding body forces.");
                        for (int dof = 0; dof < num_dofs; dof++)
                        {
                            m_inverseModel.AddUserForce(dof, new BulletSharp.Math.Vector3(0, 1, 1));
                        }


                            if (-1 != m_inverseModel.CalculateInverseDynamics(m_multiBody.HasFixedBase, q, qdot, nu, joint_force))
                        {
                            //joint_force(dof) += damping*dot_q(dof);
                            // use inverse model: apply joint force corresponding to
                            // desired acceleration nu
                            for (int dof = 0; dof < num_dofs; dof++)
                            {
                                m_multiBody.AddJointTorque(dof, joint_force[dof]);
                            }
                        } else
                        {
                            Debug.LogError("Bad return from CalculateInverseDynamics");
                        }

                    }
                    else
                    {
                        //the inverse dynamics model represents the 6 DOFs of the base, unlike btMultiBody.
                        //append some dummy values to represent the 6 DOFs of the base
                        float[] nu6 = new float[num_dofs + 6], qdot6 = new float[num_dofs + 6], q6 = new float[num_dofs + 6], joint_force6 = new float[num_dofs + 6];
                        for (int i = 0; i < num_dofs; i++)
                        {
                            nu6[6 + i] = nu[i];
                            qdot6[6 + i] = qdot[i];
                            q6[6 + i] = q[i];
                            joint_force6[6 + i] = joint_force[i];
                        }
                        if (-1 != m_inverseModel.CalculateInverseDynamics(m_multiBody.HasFixedBase, q6, qdot6, nu6, joint_force6))
                        {
                            //joint_force(dof) += damping*dot_q(dof);
                            // use inverse model: apply joint force corresponding to
                            // desired acceleration nu
                            for (int dof = 0; dof < num_dofs; dof++)
                            {
                                m_multiBody.AddJointTorque(dof, joint_force6[dof + 6]);
                            }
                        } else
                        {
                            Debug.LogError("Bad return from CalculateInverseDynamics");
                        }
                    }
                }
                else
                {
                    for (int dof = 0; dof < num_dofs; dof++)
                    {
                        // no model: just apply PD control law
                        m_multiBody.AddJointTorque(dof, pd_control[dof]);
                    }
                }
            }

            // if (m_timeSeriesCanvas)
            //     m_timeSeriesCanvas.nextTick();

            //todo: joint damping for btMultiBody, tune parameters

            // step the simulation
            if (m_dynamicsWorld != null)
            {
                // todo(thomas) check that this is correct:
                // want to advance by 10ms, with 1ms timesteps
                m_dynamicsWorld.StepSimulation(1e-3f, 0);//,1e-3);
                                                         /*
                                                         btAlignedObjectArray<BulletSharp.Math.Quaternion> scratch_q;
                                                         btAlignedObjectArray<BulletSharp.Math.Vector3> scratch_m;
                                                         m_multiBody.ForwardKinematics(scratch_q, scratch_m);
                                                         */
                //"TODO forward kinematics";
               /*
                #if 0
                        for (int i = 0; i < m_multiBody.getNumLinks(); i++)
                        {
                            //BulletSharp.Math.Vector3 pos = m_multiBody.getLink(i).m_cachedWorldTransform.getOrigin();
                            btTransform tr = m_multiBody.getLink(i).m_cachedWorldTransform;
                            BulletSharp.Math.Vector3 pos = tr.getOrigin() - quatRotate(tr.getRotation(), m_multiBody.getLink(i).m_dVector);
                            BulletSharp.Math.Vector3 localAxis = m_multiBody.getLink(i).m_axes[0].m_topVec;
                            //printf("link %d: %f,%f,%f, local axis:%f,%f,%f\n", i, pos.x(), pos.y(), pos.z(), localAxis.x(), localAxis.y(), localAxis.z());
                        }
                #endif
                */
            }
        }

        //todo(erwincoumans) Quick hack, reference to InvertedPendulumPDControl implementation. Will create a separate header/source file for this.
        public static MultiBody createInvertedPendulumMultiBody(float radius, MultiBodyDynamicsWorld world, Matrix baseWorldTrans, bool fixedBase)
        {
            BulletSharp.Math.Vector4[] colors = new BulletSharp.Math.Vector4[]
            {
            new BulletSharp.Math.Vector4(1,0,0,1),
            new BulletSharp.Math.Vector4(0,1,0,1),
            new BulletSharp.Math.Vector4(0,1,1,1),
            new BulletSharp.Math.Vector4(1,1,0,1),
            };
            int curColor = 0;

            bool damping = false;
            bool gyro = false;
            int numLinks = 2;
            bool spherical = false;                 //set it ot false -to use 1DoF hinges instead of 3DoF sphericals
            bool canSleep = false;
            bool selfCollide = false;
            BulletSharp.Math.Vector3 linkHalfExtents = new BulletSharp.Math.Vector3(0.05f, 0.37f, 0.1f);
            BulletSharp.Math.Vector3 baseHalfExtents = new BulletSharp.Math.Vector3(0.04f, 0.35f, 0.08f);


            //mbC.forceMultiDof();							//if !spherical, you can comment this line to check the 1DoF algorithm
            //init the base
            BulletSharp.Math.Vector3 baseInertiaDiag = new BulletSharp.Math.Vector3(0.0f, 0.0f, 0.0f);
            float baseMass = fixedBase ? 0.0f : 10.0f;

            if (baseMass != 0)
            {
                //CollisionShape *shape = new btSphereShape(baseHalfExtents[0]);// btBoxShape(BulletSharp.Math.Vector3(baseHalfExtents[0], baseHalfExtents[1], baseHalfExtents[2]));
                CollisionShape shape = new BoxShape(new BulletSharp.Math.Vector3(baseHalfExtents[0], baseHalfExtents[1], baseHalfExtents[2]));
                shape.CalculateLocalInertia(baseMass, out baseInertiaDiag);
                shape.Dispose();
            }


            MultiBody pMultiBody = new MultiBody(numLinks, 0, baseInertiaDiag, fixedBase, canSleep);
            pMultiBody.BaseWorldTransform = baseWorldTrans;
            BulletSharp.Math.Vector3 vel = new BulletSharp.Math.Vector3(0, 0, 0);
            //	pMultiBody.setBaseVel(vel);

            //init the links
            BulletSharp.Math.Vector3 hingeJointAxis = new BulletSharp.Math.Vector3(1, 0, 0);

            //y-axis assumed up
            BulletSharp.Math.Vector3 parentComToCurrentCom = new BulletSharp.Math.Vector3(0, -linkHalfExtents[1] * 2.0f, 0);                       //par body's COM to cur body's COM offset
            BulletSharp.Math.Vector3 currentPivotToCurrentCom = new BulletSharp.Math.Vector3(0, -linkHalfExtents[1], 0);                          //cur body's COM to cur body's PIV offset
            BulletSharp.Math.Vector3 parentComToCurrentPivot = parentComToCurrentCom - currentPivotToCurrentCom;   //par body's COM to cur body's PIV offset

            //////
            float q0 = 1.0f * Mathf.PI / 180.0f;
            BulletSharp.Math.Quaternion quat0 = new BulletSharp.Math.Quaternion(new BulletSharp.Math.Vector3(1, 0, 0), q0);
            quat0.Normalize();
            /////

            for (int i = 0; i < numLinks; ++i)
            {
                float linkMass = 1.0f;
                //if (i==3 || i==2)
                //	linkMass= 1000;
                BulletSharp.Math.Vector3 linkInertiaDiag = new BulletSharp.Math.Vector3(0.0f, 0.0f, 0.0f);

                CollisionShape shape = null;
                if (i == 0)
                {
                    shape = new BoxShape(new BulletSharp.Math.Vector3(linkHalfExtents[0], linkHalfExtents[1], linkHalfExtents[2]));//
                }
                else
                {
                    shape = new SphereShape(radius);
                }
                shape.CalculateLocalInertia(linkMass, out linkInertiaDiag);
                shape.Dispose();


                if (!spherical)
                {
                    //pMultiBody.setupRevolute(i, linkMass, linkInertiaDiag, i - 1, BulletSharp.Math.Quaternion(0.f, 0.f, 0.f, 1.f), hingeJointAxis, parentComToCurrentPivot, currentPivotToCurrentCom, false);

                    if (i == 0)
                    {
                        pMultiBody.SetupRevolute(i, linkMass, linkInertiaDiag, i - 1,
                            new BulletSharp.Math.Quaternion(0.0f, 0.0f, 0.0f, 1.0f),
                            hingeJointAxis,
                            parentComToCurrentPivot,
                            currentPivotToCurrentCom, false);
                    }
                    else
                    {
                        parentComToCurrentCom = new BulletSharp.Math.Vector3(0, -radius * 2.0f, 0);                        //par body's COM to cur body's COM offset
                        currentPivotToCurrentCom = new BulletSharp.Math.Vector3(0, -radius, 0);                          //cur body's COM to cur body's PIV offset
                        parentComToCurrentPivot = parentComToCurrentCom - currentPivotToCurrentCom;   //par body's COM to cur body's PIV offset


                        pMultiBody.SetupFixed(i, linkMass, linkInertiaDiag, i - 1,
                                        new BulletSharp.Math.Quaternion(0.0f, 0.0f, 0.0f, 1.0f),
                                        parentComToCurrentPivot,
                                        currentPivotToCurrentCom);
                    }
                }
                else
                {
                    //pMultiBody.setupPlanar(i, linkMass, linkInertiaDiag, i - 1, BulletSharp.Math.Quaternion(0.f, 0.f, 0.f, 1.f)/*quat0*/, BulletSharp.Math.Vector3(1, 0, 0), parentComToCurrentPivot*2, false);
                    pMultiBody.SetupSpherical(i, linkMass, linkInertiaDiag, i - 1, new BulletSharp.Math.Quaternion(0.0f, 0.0f, 0.0f, 1.0f), parentComToCurrentPivot, currentPivotToCurrentCom, false);
                }
            }

            pMultiBody.FinalizeMultiDof();
            world.AddMultiBody(pMultiBody);
            MultiBody mbC = pMultiBody;
            mbC.CanSleep = (canSleep);
            mbC.HasSelfCollision = (selfCollide);
            mbC.UseGyroTerm = (gyro);
            //
            if (!damping)
            {
                mbC.LinearDamping = (0.0f);
                mbC.AngularDamping = (0.0f);
            }
            else
            {
                mbC.LinearDamping = (0.1f);
                mbC.AngularDamping = (0.9f);
            }


            if (numLinks > 0)
            {
                q0 = 180.0f * Mathf.PI / 180.0f;
                if (!spherical)
                {
                    mbC.SetJointPosMultiDof(0, new float[] { q0 });
                }
                else
                {
                    BulletSharp.Math.Vector3 vv = new BulletSharp.Math.Vector3(1, 1, 0);
                    vv.Normalize();
                    quat0 = new BulletSharp.Math.Quaternion(vv, q0);
                    quat0.Normalize();
                    float[] quat0fs = new float[] { quat0.X, quat0.Y, quat0.Z, quat0.W };
                    mbC.SetJointPosMultiDof(0, quat0fs);
                }
            }
            ///
            BulletSharp.Math.Quaternion[] world_to_local; //btAlignedObjectArray<BulletSharp.Math.Quaternion>
            world_to_local = new BulletSharp.Math.Quaternion[pMultiBody.NumLinks + 1];

            BulletSharp.Math.Vector3[] local_origin; //btAlignedObjectArray<BulletSharp.Math.Vector3>
            local_origin = new BulletSharp.Math.Vector3[pMultiBody.NumLinks + 1];
            world_to_local[0] = pMultiBody.WorldToBaseRot;
            local_origin[0] = pMultiBody.BasePosition;
            //  double friction = 1;
            {
                if (true)
                {
                    CollisionShape shape = new BoxShape(new BulletSharp.Math.Vector3(baseHalfExtents[0], baseHalfExtents[1], baseHalfExtents[2]));//new btSphereShape(baseHalfExtents[0]);
                                                                                                                                                  // guiHelper.createCollisionShapeGraphicsObject(shape);

                    MultiBodyLinkCollider col = new MultiBodyLinkCollider(pMultiBody, -1);
                    col.CollisionShape = shape;

                    Matrix tr = new Matrix();
                    tr.ScaleVector = BulletSharp.Math.Vector3.One;
                    //if we don't set the initial pose of the btCollisionObject, the simulator will do this 
                    //when syncing the btMultiBody link transforms to the btMultiBodyLinkCollider

                    tr.Origin = local_origin[0];
                    BulletSharp.Math.Quaternion orn = new BulletSharp.Math.Quaternion(new BulletSharp.Math.Vector3(0, 0, 1), 0.25f * 3.1415926538f);

                    tr.Rotation = (orn);
                    col.WorldTransform = (tr);

                    bool isDynamic = (baseMass > 0 && !fixedBase);
                    CollisionFilterGroups collisionFilterGroup = isDynamic ? CollisionFilterGroups.DefaultFilter : CollisionFilterGroups.StaticFilter;
                    CollisionFilterGroups collisionFilterMask = isDynamic ? CollisionFilterGroups.AllFilter : CollisionFilterGroups.AllFilter ^ CollisionFilterGroups.StaticFilter;


                    world.AddCollisionObject(col, collisionFilterGroup, collisionFilterMask);//, 2,1+2);

                    BulletSharp.Math.Vector4 color = new BulletSharp.Math.Vector4(0.0f, 0.0f, 0.5f, 1f);
                    //guiHelper.createCollisionObjectGraphicsObject(col, color);

                    //                col.setFriction(friction);
                    pMultiBody.BaseCollider = (col);

                }
            }


            for (int i = 0; i < pMultiBody.NumLinks; ++i)
            {
                int parent = pMultiBody.GetParent(i);
                world_to_local[i + 1] = pMultiBody.GetParentToLocalRot(i) * world_to_local[parent + 1];
                BulletSharp.Math.Vector3 vv = world_to_local[i + 1].Inverse.Rotate(pMultiBody.GetRVector(i));
                local_origin[i + 1] = local_origin[parent + 1] + vv;
            }


            for (int i = 0; i < pMultiBody.NumLinks; ++i)
            {

                BulletSharp.Math.Vector3 posr = local_origin[i + 1];
                //	float pos[4]={posr.x(),posr.y(),posr.z(),1};

                float[] quat = new float[] { -world_to_local[i + 1].X, -world_to_local[i + 1].Y, -world_to_local[i + 1].Z, world_to_local[i + 1].W };
                CollisionShape shape = null;

                if (i == 0)
                {
                    shape = new BoxShape(new BulletSharp.Math.Vector3(linkHalfExtents[0], linkHalfExtents[1], linkHalfExtents[2]));//btSphereShape(linkHalfExtents[0]);
                }
                else
                {

                    shape = new SphereShape(radius);
                }

                //guiHelper.createCollisionShapeGraphicsObject(shape);
                MultiBodyLinkCollider col = new MultiBodyLinkCollider(pMultiBody, i);
                col.CollisionShape = (shape);
                Matrix tr = new Matrix();
                tr.ScaleVector = new BulletSharp.Math.Vector3();
                tr.Origin = (posr);
                tr.Rotation = (new BulletSharp.Math.Quaternion(quat[0], quat[1], quat[2], quat[3]));
                col.WorldTransform = (tr);
                //       col.setFriction(friction);
                bool isDynamic = true;//(linkMass > 0);
                CollisionFilterGroups collisionFilterGroup = isDynamic ? CollisionFilterGroups.DefaultFilter : CollisionFilterGroups.StaticFilter;
                CollisionFilterGroups collisionFilterMask = isDynamic ? CollisionFilterGroups.AllFilter : CollisionFilterGroups.AllFilter ^ CollisionFilterGroups.StaticFilter;

                //if (i==0||i>numLinks-2)
                {
                    world.AddCollisionObject(col, collisionFilterGroup, collisionFilterMask);//,2,1+2);
                    BulletSharp.Math.Vector4 color = colors[curColor];
                    curColor++;
                    curColor &= 3;
                    //guiHelper.createCollisionObjectGraphicsObject(col, color);


                    pMultiBody.GetLink(i).Collider = col;
                }

            }

            return pMultiBody;
        }
    }
}
