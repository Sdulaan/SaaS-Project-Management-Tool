import { useEffect, useMemo, useState } from 'react';
import type { FormEvent } from 'react';
import { DragDropContext, Droppable, Draggable, type DropResult } from 'react-beautiful-dnd';
import './index.css';

type AuthMode = 'login' | 'register';

type AuthResponse = {
    token: string;
    userId: string;
    organizationId: string;
    email: string;
    fullName: string;
    role: string;
};

type Project = {
    id: string;
    name: string;
    description: string | null;
    dueDateUtc: string | null;
    totalTasks: number;
    completedTasks: number;
    isCompleted?: boolean;
};

type CompletedProject = {
    id: string;
    name: string;
    description: string | null;
    dueDateUtc: string | null;
    totalTasks: number;
    completedTasks: number;
    memberCount: number;
    completedAtUtc: string;
};

type WorkItem = {
    id: string;
    projectId: string;
    title: string;
    description: string | null;
    status: number;
    priority: number;
    assigneeId: string | null;
    assigneeName: string | null;
    assigneeEmail: string | null;
    dueDateUtc: string | null;
    storyPoints: number;
};

type Member = {
    id: string;
    fullName: string;
    displayName: string;
    email: string;
    role: string;
};

type DashboardSummary = {
    totalProjects: number;
    totalTasks: number;
    completedTasks: number;
    inProgressTasks: number;
    overdueTasks: number;
};

const API_URL = import.meta.env.VITE_API_URL ?? 'https://localhost:7068';

const statusLabels: Record<number, string> = {
    1: 'Backlog',
    2: 'Todo',
    3: 'In Progress',
    4: 'In Review',
    5: 'Done',
};

const priorityLabels: Record<number, string> = {
    1: 'Low',
    2: 'Medium',
    3: 'High',
    4: 'Critical',
    5: 'System Break',
};

const priorityColors: Record<number, string> = {
    1: '#4ade80',
    2: '#facc15',
    3: '#fb923c',
    4: '#f87171',
    5: '#c084fc',
};

const columns = [1, 2, 3, 4, 5];

function App() {
    const [authMode, setAuthMode] = useState<AuthMode>('login');
    const [auth, setAuth] = useState<AuthResponse | null>(null);
    const [error, setError] = useState('');
    const [loading, setLoading] = useState(false);

    const [projects, setProjects] = useState<Project[]>([]);
    const [completedProjects, setCompletedProjects] = useState<CompletedProject[]>([]);
    const [activeTab, setActiveTab] = useState<'active' | 'completed'>('active');
    const [summary, setSummary] = useState<DashboardSummary | null>(null);
    const [selectedProjectId, setSelectedProjectId] = useState<string>('');
    const [workItems, setWorkItems] = useState<WorkItem[]>([]);

    const [showProjectModal, setShowProjectModal] = useState(false);
    const [showTaskModal, setShowTaskModal] = useState(false);
    const [showMembersModal, setShowMembersModal] = useState(false);

    const [projectName, setProjectName] = useState('');
    const [projectDescription, setProjectDescription] = useState('');
    const [taskTitle, setTaskTitle] = useState('');
    const [taskDescription, setTaskDescription] = useState('');
    const [taskPriority, setTaskPriority] = useState(2);
    const [taskAssigneeId, setTaskAssigneeId] = useState<string>('');

    const [members, setMembers] = useState<Member[]>([]);
    const [newMemberEmail, setNewMemberEmail] = useState('');
    const [newMemberName, setNewMemberName] = useState('');
    const [newMemberDisplayName, setNewMemberDisplayName] = useState('');

    useEffect(() => {
        const cached = localStorage.getItem('spm_auth');
        if (cached) setAuth(JSON.parse(cached) as AuthResponse);
    }, []);

    useEffect(() => {
        setError('');
    }, [authMode]);

    useEffect(() => {
        if (!auth) return;
        localStorage.setItem('spm_auth', JSON.stringify(auth));
        void refreshDashboard(auth.token);
        void loadMembers(auth.token);
    }, [auth]);

    useEffect(() => {
        if (auth && selectedProjectId) {
            void loadWorkItems(auth.token, selectedProjectId);
        }
    }, [auth, selectedProjectId]);

    // Close modal on Escape key
    useEffect(() => {
        function onKey(e: KeyboardEvent) {
            if (e.key === 'Escape') {
                setShowProjectModal(false);
                setShowTaskModal(false);
                setShowMembersModal(false);
            }
        }
        window.addEventListener('keydown', onKey);
        return () => window.removeEventListener('keydown', onKey);
    }, []);

    const groupedItems = useMemo(() => {
        const map = new Map<number, WorkItem[]>();
        columns.forEach((c) => map.set(c, []));
        for (const item of workItems) {
            if (!map.has(item.status)) map.set(item.status, []);
            map.get(item.status)?.push(item);
        }
        return map;
    }, [workItems]);

    const selectedProject = projects.find((p) => p.id === selectedProjectId);

    async function api<T>(path: string, method = 'GET', body?: unknown, token?: string): Promise<T> {
        const response = await fetch(`${API_URL}${path}`, {
            method,
            headers: {
                'Content-Type': 'application/json',
                ...(token ? { Authorization: `Bearer ${token}` } : {}),
            },
            body: body ? JSON.stringify(body) : undefined,
        });

        if (!response.ok) {
            const payload = (await response.json().catch(() => ({ error: 'Request failed' }))) as { error?: string };
            throw new Error(payload.error ?? 'Request failed');
        }

        if (response.status === 204) return {} as T;
        return (await response.json()) as T;
    }

    async function refreshDashboard(token: string) {
        const [projectData, summaryData, completedProjectData] = await Promise.all([
            api<Project[]>('/api/projects', 'GET', undefined, token),
            api<DashboardSummary>('/api/dashboard/summary', 'GET', undefined, token),
            api<CompletedProject[]>('/api/projects/completed', 'GET', undefined, token),
        ]);

        setProjects(projectData);
        setCompletedProjects(completedProjectData);
        setSummary(summaryData);

        if (projectData.length > 0) {
            const nextProjectId = selectedProjectId || projectData[0].id;
            setSelectedProjectId(nextProjectId);
            await loadWorkItems(token, nextProjectId);
        } else {
            setSelectedProjectId('');
            setWorkItems([]);
        }
    }

    async function loadWorkItems(token: string, projectId: string) {
        const items = await api<WorkItem[]>(`/api/work-items/project/${projectId}`, 'GET', undefined, token);
        setWorkItems(items);
    }

    async function onAuthSubmit(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        setError('');
        setLoading(true);
        try {
            const form = new FormData(event.currentTarget);
            const payload = authMode === 'register'
                ? {
                    organizationName: String(form.get('organizationName') ?? ''),
                    fullName: String(form.get('fullName') ?? ''),
                    email: String(form.get('email') ?? ''),
                    password: String(form.get('password') ?? ''),
                }
                : {
                    email: String(form.get('email') ?? ''),
                    password: String(form.get('password') ?? ''),
                };

            const endpoint = authMode === 'register' ? '/api/auth/register' : '/api/auth/login';
            const result = await api<AuthResponse>(endpoint, 'POST', payload);
            setAuth(result);
        } catch (e) {
            setError(e instanceof Error ? e.message : 'Authentication failed');
        } finally {
            setLoading(false);
        }
    }

    async function onCreateProject(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        if (!auth) return;
        setError('');
        try {
            await api('/api/projects', 'POST', { name: projectName, description: projectDescription || null, dueDateUtc: null }, auth.token);
            setProjectName('');
            setProjectDescription('');
            setShowProjectModal(false);
            await refreshDashboard(auth.token);
        } catch (e) {
            setError(e instanceof Error ? e.message : 'Failed to create project');
        }
    }

    async function onCreateTask(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        if (!auth || !selectedProjectId || !taskTitle.trim()) return;
        setError('');
        try {
            await api('/api/work-items', 'POST', {
                projectId: selectedProjectId,
                title: taskTitle,
                description: taskDescription || null,
                assigneeId: taskAssigneeId ? taskAssigneeId : null,
                priority: taskPriority,
                dueDateUtc: null,
                storyPoints: 3,
            }, auth.token);

            setTaskTitle('');
            setTaskDescription('');
            setTaskPriority(2);
            setTaskAssigneeId('');
            setShowTaskModal(false);
            await loadWorkItems(auth.token, selectedProjectId);
            await refreshDashboard(auth.token);
        } catch (e) {
            setError(e instanceof Error ? e.message : 'Failed to create task');
        }
    }

    async function loadMembers(token: string) {
        try {
            const memberList = await api<Member[]>('/api/members', 'GET', undefined, token);
            setMembers(memberList);
        } catch (e) {
            console.error('Failed to load members:', e);
        }
    }

    async function onAddMember(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        if (!auth || !newMemberEmail.trim() || !newMemberName.trim() || !newMemberDisplayName.trim()) return;
        setError('');
        try {
            await api('/api/members', 'POST', { fullName: newMemberName, displayName: newMemberDisplayName, email: newMemberEmail }, auth.token);
            setNewMemberEmail('');
            setNewMemberName('');
            setNewMemberDisplayName('');
            await loadMembers(auth.token);
        } catch (e) {
            setError(e instanceof Error ? e.message : 'Failed to add member');
        }
    }

    async function onRemoveMember(userId: string) {
        if (!auth || !confirm('Are you sure you want to remove this member?')) return;
        try {
            await api(`/api/members/${userId}`, 'DELETE', undefined, auth.token);
            await loadMembers(auth.token);
        } catch (e) {
            setError(e instanceof Error ? e.message : 'Failed to remove member');
        }
    }

    async function updateTaskAssignee(item: WorkItem, newAssigneeId: string | null) {
        if (!auth) return;
        try {
            await api(`/api/work-items/${item.id}/assignee`, 'PATCH', { assigneeId: newAssigneeId || null }, auth.token);
            await loadWorkItems(auth.token, item.projectId);
        } catch (e) {
            setError(e instanceof Error ? e.message : 'Failed to update assignee');
        }
    }

    async function moveTask(item: WorkItem, nextStatus: number) {
        if (!auth) return;
        await api(`/api/work-items/${item.id}/status`, 'PATCH', { status: nextStatus }, auth.token);
        await loadWorkItems(auth.token, item.projectId);
        await refreshDashboard(auth.token);
    }

    async function completeProject(projectId: string) {
        if (!auth) return;
        setError('');
        try {
            await api(`/api/projects/${projectId}/complete`, 'PATCH', {}, auth.token);
            await refreshDashboard(auth.token);
        } catch (e) {
            setError(e instanceof Error ? e.message : 'Failed to complete project');
        }
    }

    async function handleDragEnd(result: DropResult) {
        const { source, destination, draggableId } = result;

        if (!destination) return;
        if (source.droppableId === destination.droppableId && source.index === destination.index) return;

        const newStatus = Number(destination.droppableId);
        const task = workItems.find((w) => w.id === draggableId);

        if (task) {
            await moveTask(task, newStatus);
        }
    }

    function handleSelectProject(projectId: string) {
        setSelectedProjectId(projectId);
        if (auth) void loadWorkItems(auth.token, projectId);
    }

    if (!auth) {
        return (
            <div className="auth-page">
                <div className="glow glow-a" />
                <div className="glow glow-b" />
                <div className="auth-card">
                    <h1>Project Orbit</h1>
                    <p>Multi-tenant SaaS workspace for enterprise teams.</p>
                    <form onSubmit={onAuthSubmit} key={authMode}>
                        {authMode === 'register' && (
                            <>
                                <input name="organizationName" placeholder="Organization name" required />
                                <input name="fullName" placeholder="Full name" required />
                            </>
                        )}
                        <input type="email" name="email" placeholder="Work email" required />
                        <input type="password" name="password" placeholder="Password" required minLength={6} />
                        {error && <div className="error">{error}</div>}
                        <button disabled={loading}>{loading ? 'Please wait...' : authMode === 'register' ? 'Create Workspace' : 'Sign In'}</button>
                    </form>
                    <button className="ghost" onClick={() => setAuthMode(authMode === 'login' ? 'register' : 'login')}>
                        {authMode === 'login' ? 'New here? Register organization' : 'Already have an account? Login'}
                    </button>
                </div>
            </div>
        );
    }

    return (
        <div className="dashboard-page">
            <aside className="sidebar">
                <h2>Project Orbit</h2>
                <p>{auth.fullName}</p>
                <small>{auth.role} | {auth.email}</small>
                <button
                    className="ghost"
                    onClick={() => {
                        localStorage.removeItem('spm_auth');
                        setAuth(null);
                    }}
                >
                    Sign out
                </button>
            </aside>

            <main className="content">
                <header className="hero">
                    <div className="hero-top">
                        <div>
                            <h1>Execution Dashboard</h1>
                            <p>Focused view of projects, throughput, and delivery risk.</p>
                        </div>
                        <div className="hero-actions">
                            <button className="btn-outline" onClick={() => setShowMembersModal(true)}>+ Members</button>
                            <button className="btn-outline" onClick={() => setShowProjectModal(true)}>+ New Project</button>
                            <button className="btn-outline" onClick={() => setShowTaskModal(true)}>+ New Task</button>
                            {selectedProjectId && !selectedProject?.isCompleted && (
                                <button
                                    className="btn-outline"
                                    onClick={() => completeProject(selectedProjectId)}
                                    style={{ color: '#4ade80' }}
                                >
                                    ✓ Complete Project
                                </button>
                            )}
                        </div>
                    </div>
                </header>

                {/* Stats */}
                <section className="stats-grid">
                    <article><span>Projects</span><strong>{summary?.totalProjects ?? 0}</strong></article>
                    <article><span>Tasks</span><strong>{summary?.totalTasks ?? 0}</strong></article>
                    <article><span>Completed</span><strong>{summary?.completedTasks ?? 0}</strong></article>
                    <article><span>Overdue</span><strong>{summary?.overdueTasks ?? 0}</strong></article>
                </section>

                {/* Projects */}
                <section className="panel">
                    <div style={{ display: 'flex', gap: '12px', marginBottom: '16px' }}>
                        <button
                            className={`btn-outline ${activeTab === 'active' ? 'active' : ''}`}
                            onClick={() => setActiveTab('active')}
                            style={{
                                background: activeTab === 'active' ? '#2a9d8f' : 'transparent',
                                borderColor: activeTab === 'active' ? '#2a9d8f' : '#444',
                            }}
                        >
                            Active Projects
                        </button>
                        <button
                            className={`btn-outline ${activeTab === 'completed' ? 'active' : ''}`}
                            onClick={() => setActiveTab('completed')}
                            style={{
                                background: activeTab === 'completed' ? '#2a9d8f' : 'transparent',
                                borderColor: activeTab === 'completed' ? '#2a9d8f' : '#444',
                            }}
                        >
                            Completed Projects ({completedProjects.length})
                        </button>
                    </div>

                    {activeTab === 'active' ? (
                        <>
                            <h3 style={{ marginTop: 0 }}>Projects</h3>
                            {projects.length === 0 ? (
                                <p className="muted">No active projects yet. Click <strong>+ New Project</strong> to get started.</p>
                            ) : (
                                <div className="project-list">
                                    {projects.map((project) => (
                                        <button
                                            key={project.id}
                                            className={`project-btn${project.id === selectedProjectId ? ' project-btn--active' : ''}`}
                                            onClick={() => handleSelectProject(project.id)}
                                        >
                                            <span className="project-btn-name">{project.name}</span>
                                            <span className="project-btn-desc">{project.description || 'No description'}</span>
                                            <span className="project-btn-meta">{project.completedTasks}/{project.totalTasks} tasks completed</span>
                                        </button>
                                    ))}
                                </div>
                            )}
                        </>
                    ) : (
                        <>
                            <h3 style={{ marginTop: 0 }}>Completed Projects</h3>
                            {completedProjects.length === 0 ? (
                                <p className="muted">No completed projects yet. Complete all tasks in a project to mark it as done.</p>
                            ) : (
                                <div className="project-list">
                                    {completedProjects.map((project) => (
                                        <div
                                            key={project.id}
                                            className="project-btn"
                                            style={{ cursor: 'default', opacity: 0.7 }}
                                        >
                                            <span className="project-btn-name">{project.name}</span>
                                            <span className="project-btn-desc">{project.description || 'No description'}</span>
                                            <div style={{ fontSize: '0.85rem', color: '#888', marginTop: '4px' }}>
                                                {project.memberCount} members worked on this • {project.totalTasks} total tasks
                                            </div>
                                        </div>
                                    ))}
                                </div>
                            )}
                        </>
                    )}
                </section>

                {/* Kanban board */}
                {selectedProjectId && (
                    <section className="panel kanban-panel">
                        <div className="kanban-header">
                            <h3>{selectedProject?.name} — Board</h3>
                            <span className="kanban-header-meta">{selectedProject?.completedTasks}/{selectedProject?.totalTasks} tasks completed</span>
                        </div>
                        <DragDropContext onDragEnd={handleDragEnd}>
                            <div className="kanban">
                                {columns.map((status) => (
                                    <Droppable key={status} droppableId={String(status)}>
                                        {(provided, snapshot) => (
                                            <div
                                                className="kanban-column"
                                                ref={provided.innerRef}
                                                {...provided.droppableProps}
                                                style={{
                                                    background: snapshot.isDraggingOver ? '#1f2937' : 'transparent',
                                                    borderRadius: '4px',
                                                    transition: 'background 0.2s'
                                                } as React.CSSProperties}
                                            >
                                                <h4>{statusLabels[status]}</h4>
                                                <div className="kanban-list">
                                                    {(groupedItems.get(status) ?? []).length === 0 ? (
                                                        <p className="muted" style={{ fontSize: '0.85rem', padding: '4px' }}>Empty</p>
                                                    ) : (
                                                        (groupedItems.get(status) ?? []).map((item, index) => (
                                                            <Draggable key={item.id} draggableId={item.id} index={index}>
                                                                {(provided, snapshot) => (
                                                                    <article
                                                                        className="ticket"
                                                                        ref={provided.innerRef}
                                                                        {...provided.draggableProps}
                                                                        {...provided.dragHandleProps}
                                                                        style={{
                                                                            ...provided.draggableProps.style,
                                                                            background: snapshot.isDragging ? '#2a2a3e' : '#0f0f1e',
                                                                            boxShadow: snapshot.isDragging ? '0 4px 12px rgba(0,0,0,0.5)' : 'none',
                                                                            cursor: snapshot.isDragging ? 'grabbing' : 'grab',
                                                                        }}
                                                                    >
                                                                        <h5>{item.title}</h5>
                                                                        <p>{item.description || 'No description'}</p>
                                                                        <div style={{ marginBottom: '8px', fontSize: '0.85rem' }}>
                                                                            <strong>Assigned To:</strong>{' '}
                                                                            <select
                                                                                value={item.assigneeId || ''}
                                                                                onChange={(e) => updateTaskAssignee(item, e.target.value || null)}
                                                                                style={{ padding: '4px', marginLeft: '4px' }}
                                                                                onClick={(e) => e.stopPropagation()}
                                                                            >
                                                                                <option value="">Unassigned</option>
                                                                                {members.map((member) => (
                                                                                    <option key={member.id} value={member.id}>{member.displayName}</option>
                                                                                ))}
                                                                            </select>
                                                                        </div>
                                                                        <div className="ticket-row">
                                                                            <span
                                                                                style={{
                                                                                    background: priorityColors[item.priority] ?? '#888',
                                                                                    color: '#000',
                                                                                    padding: '2px 8px',
                                                                                    borderRadius: '4px',
                                                                                    fontSize: '0.75rem',
                                                                                    fontWeight: 600,
                                                                                }}
                                                                            >
                                                                                {priorityLabels[item.priority] ?? `P${item.priority}`}
                                                                            </span>
                                                                            <span style={{ fontSize: '0.8rem', color: '#888' }}>
                                                                                {statusLabels[item.status]}
                                                                            </span>
                                                                        </div>
                                                                    </article>
                                                                )}
                                                            </Draggable>
                                                        ))
                                                    )}
                                                    {provided.placeholder}
                                                </div>
                                            </div>
                                        )}
                                    </Droppable>
                                ))}
                            </div>
                        </DragDropContext>
                    </section>
                )}
            </main>

            {/* Create Project Modal */}
            {showProjectModal && (
                <div className="modal-backdrop" onClick={() => setShowProjectModal(false)}>
                    <div className="modal" onClick={(e) => e.stopPropagation()}>
                        <div className="modal-header">
                            <h3>Create Project</h3>
                            <button className="modal-close" onClick={() => setShowProjectModal(false)}>✕</button>
                        </div>
                        <form onSubmit={onCreateProject}>
                            <input value={projectName} onChange={(e) => setProjectName(e.target.value)} placeholder="Project name" required />
                            <input value={projectDescription} onChange={(e) => setProjectDescription(e.target.value)} placeholder="Description (optional)" />
                            {error && <div className="error">{error}</div>}
                            <div className="modal-actions">
                                <button type="button" className="ghost" onClick={() => setShowProjectModal(false)}>Cancel</button>
                                <button type="submit">Create Project</button>
                            </div>
                        </form>
                    </div>
                </div>
            )}

            {/* Create Task Modal */}
            {showTaskModal && (
                <div className="modal-backdrop" onClick={() => { setShowTaskModal(false); setTaskAssigneeId(''); }}>
                    <div className="modal" onClick={(e) => e.stopPropagation()}>
                        <div className="modal-header">
                            <h3>Create Task</h3>
                            <button className="modal-close" onClick={() => { setShowTaskModal(false); setTaskAssigneeId(''); }}>✕</button>
                        </div>
                        <form onSubmit={onCreateTask}>
                            <select value={selectedProjectId} onChange={(e) => setSelectedProjectId(e.target.value)}>
                                {projects.map((project) => (
                                    <option value={project.id} key={project.id}>{project.name}</option>
                                ))}
                            </select>
                            <input value={taskTitle} onChange={(e) => setTaskTitle(e.target.value)} placeholder="Task title" required />
                            <input value={taskDescription} onChange={(e) => setTaskDescription(e.target.value)} placeholder="Description (optional)" />
                            <select value={taskPriority} onChange={(e) => setTaskPriority(Number(e.target.value))}>
                                {Object.entries(priorityLabels).map(([value, label]) => (
                                    <option key={value} value={value}>{label}</option>
                                ))}
                            </select>
                            <select value={taskAssigneeId} onChange={(e) => setTaskAssigneeId(e.target.value)}>
                                <option value="">Unassigned</option>
                                {members.map((member) => (
                                    <option key={member.id} value={member.id}>{member.displayName}</option>
                                ))}
                            </select>
                            {error && <div className="error">{error}</div>}
                            <div className="modal-actions">
                                <button type="button" className="ghost" onClick={() => { setShowTaskModal(false); setTaskAssigneeId(''); }}>Cancel</button>
                                <button type="submit">Create Task</button>
                            </div>
                        </form>
                    </div>
                </div>
            )}

            {/* Members Modal */}
            {showMembersModal && (
                <div className="modal-backdrop" onClick={() => setShowMembersModal(false)}>
                    <div className="modal" onClick={(e) => e.stopPropagation()} style={{ maxWidth: '500px', maxHeight: '600px', overflowY: 'auto' }}>
                        <div className="modal-header">
                            <h3>Organization Members</h3>
                            <button className="modal-close" onClick={() => setShowMembersModal(false)}>✕</button>
                        </div>
                        <div style={{ padding: '16px' }}>
                            <form onSubmit={onAddMember} style={{ marginBottom: '16px' }}>
                                <div style={{ display: 'flex', gap: '8px', marginBottom: '8px' }}>
                                    <input
                                        type="text"
                                        value={newMemberName}
                                        onChange={(e) => setNewMemberName(e.target.value)}
                                        placeholder="Full name"
                                        required
                                        style={{ flex: 1 }}
                                    />
                                </div>
                                <div style={{ display: 'flex', gap: '8px', marginBottom: '8px' }}>
                                    <input
                                        type="text"
                                        value={newMemberDisplayName}
                                        onChange={(e) => setNewMemberDisplayName(e.target.value)}
                                        placeholder="Display name (shown in dropdowns)"
                                        required
                                        style={{ flex: 1 }}
                                    />
                                </div>
                                <div style={{ display: 'flex', gap: '8px' }}>
                                    <input
                                        type="email"
                                        value={newMemberEmail}
                                        onChange={(e) => setNewMemberEmail(e.target.value)}
                                        placeholder="Email address"
                                        required
                                        style={{ flex: 1 }}
                                    />
                                    <button type="submit">Add Member</button>
                                </div>
                            </form>
                            {error && <div className="error" style={{ marginBottom: '16px' }}>{error}</div>}
                            <div>
                                <h4 style={{ marginTop: 0 }}>Members ({members.length})</h4>
                                {members.length === 0 ? (
                                    <p className="muted">No members yet.</p>
                                ) : (
                                    <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
                                        {members.map((member) => (
                                            <div
                                                key={member.id}
                                                style={{
                                                    display: 'flex',
                                                    justifyContent: 'space-between',
                                                    alignItems: 'center',
                                                    padding: '8px',
                                                    backgroundColor: '#1a1a2e',
                                                    borderRadius: '4px',
                                                }}
                                            >
                                                <div>
                                                    <div style={{ fontWeight: 600 }}>{member.fullName}</div>
                                                    <div style={{ fontSize: '0.85rem', color: '#888' }}>{member.email}</div>
                                                </div>
                                                <button
                                                    className="ghost"
                                                    onClick={() => onRemoveMember(member.id)}
                                                    style={{ color: '#f87171' }}
                                                >
                                                    Remove
                                                </button>
                                            </div>
                                        ))}
                                    </div>
                                )}
                            </div>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}

export default App;